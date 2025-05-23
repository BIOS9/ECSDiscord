﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MySqlConnector;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace ECSDiscord.Services.Storage;

public class StorageService : IHostedService
{
    private static readonly DateTime Epoch = new(1970, 1, 1);
    private static readonly TimeSpan CleanupPeriod = TimeSpan.FromDays(1);
    private readonly StorageOptions _options;
    private readonly string _mysqlConnectionString;

    public StorageService(IOptions<StorageOptions> options)
    {
        Log.Debug("Storage service loading.");
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _mysqlConnectionString = _options.ConnectionString;
        Verification = new VerificationStorage(this);
        Users = new UserStorage(this);
        Courses = new CourseStorage(this);
        ServerMessages = new ServerMessageStorage(this);
        Minecraft = new MinecraftStorage(this);
        Log.Debug("Storage service loaded.");
    }

    public VerificationStorage Verification { get; }


    public UserStorage Users { get; }
    
    public CourseStorage Courses { get; }

    public ServerMessageStorage ServerMessages { get; }

    public MinecraftStorage Minecraft { get; }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Log.Debug("Testing MySql connection.");
        using (var con = GetMySqlConnection())
        {
            await con.OpenAsync();
            Log.Information("Connected to MySql version: {version}", con.ServerVersion);
        }

        Log.Debug("MySql connection test succeeded.");
        startCleanupService();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task<string> GetUserDataAsync(ulong user)
    {
        var pendingVerifications = await Verification.GetAllUserPendingVerifications(user);
        var courses = await Users.GetUserCoursesAsync(user);
        var currentUsername = await Users.GetEncryptedUsernameAsync(user);
        var verificationHistory = await Verification.GetUserVerificationHistoryAsync(user);

        var o = JObject.FromObject(new
        {
            user_id = user,
            current_username = currentUsername,
            joined_courses = courses,
            pending_verifications = pendingVerifications.Select(x => new
                { encrypted_username = x.EncryptedUsername, creation_time = x.CreationTime }),
            verification_history = verificationHistory.Select(x => new
                { encrypted_username = x.EncryptedUsername, verification_time = x.VerificationTime })
        });
        return o.ToString(Formatting.Indented);
    }

    protected MySqlConnection GetMySqlConnection()
    {
        return new MySqlConnection(_mysqlConnectionString);
    }

    public async Task Cleanup()
    {
        try
        {
            Log.Information("Database cleanup executed.");
            await Verification.Cleanup();
            Log.Debug("Database cleanup finished.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error while running database cleanup: {message}", ex.Message);
        }
    }

    private async void startCleanupService()
    {
        await Task.Delay(TimeSpan.FromMinutes(5));
        Log.Debug("Storage cleanup service started.");
        while (true)
        {
            await Cleanup();
            await Task.Delay(CleanupPeriod);
        }
    }

    public class DuplicateRecordException : Exception
    {
        public DuplicateRecordException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public DuplicateRecordException(string message) : base(message)
        {
        }

        public DuplicateRecordException()
        {
        }
    }

    public class RecordNotFoundException : Exception
    {
        public RecordNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public RecordNotFoundException(string message) : base(message)
        {
        }

        public RecordNotFoundException()
        {
        }
    }

    public class VerificationStorage
    {
        public enum OverrideType
        {
            USER,
            ROLE
        }

        private const string PendingVerificationsTable = "pendingverifications";
        private const string VerificationHistoryTable = "verificationhistory";
        private const string VerificationOverrideTable = "verificationoverrides";
        private const string UsersTable = "users";
        private static readonly TimeSpan PendingVerificationDeletionTime = TimeSpan.FromDays(14);

        private readonly StorageService _storageService;

        public VerificationStorage(StorageService storageService)
        {
            _storageService = storageService;
        }

        /// <summary>
        ///     Add pending verification token to storage.
        /// </summary>
        public async Task AddPendingVerificationAsync(string token, byte[] encryptedUsername, ulong discordId)
        {
            try
            {
                Log.Debug("Adding pending verification to database for {id}", discordId);
                var rowsAffected = 0;
                using (var con = _storageService.GetMySqlConnection())
                using (var cmd = new MySqlCommand())
                {
                    await con.OpenAsync();
                    cmd.Connection = con;

                    cmd.CommandText = $"INSERT INTO `{PendingVerificationsTable}` " +
                                      "(`token`, `encryptedUsername`, `discordSnowflake`, `creationTime`) " +
                                      "VALUES (@token, @encryptedUsername, @discordId, @time);";
                    cmd.Prepare();

                    cmd.Parameters.AddWithValue("@token", token);
                    cmd.Parameters.AddWithValue("@encryptedUsername", encryptedUsername);
                    cmd.Parameters.AddWithValue("@discordId", discordId);

                    var t = DateTime.UtcNow - Epoch;
                    cmd.Parameters.AddWithValue("@time", (long)t.TotalSeconds);

                    rowsAffected = await cmd.ExecuteNonQueryAsync();
                    Log.Debug(
                        "Successfully added pending verification to database for {id}. Rows affected: {rowsAffected}",
                        discordId, rowsAffected);
                }
            }
            catch (MySqlException ex)
            {
                if (ex.Message.StartsWith("Duplicate entry"))
                    throw new DuplicateRecordException("Duplicate verification code.");
                throw ex;
            }
        }

        public async Task<PendingVerification> GetPendingVerificationAsync(string token)
        {
            Log.Debug("Getting pending verification from database.");
            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                cmd.Connection = con;

                cmd.CommandText = "SELECT `encryptedUsername`, `discordSnowflake`, `creationTime` " +
                                  $"FROM `{PendingVerificationsTable}` WHERE `token` = @token;";
                cmd.Parameters.AddWithValue("@token", token);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        var length = (int)reader.GetBytes(0, 0, null, 0, 0);
                        var encryptedUsername = new byte[length];
                        var index = 0;

                        while (index < length)
                        {
                            var bytesRead = (int)reader.GetBytes(0, index,
                                encryptedUsername, index, length - index);
                            index += bytesRead;
                        }

                        var discordId = reader.GetUInt64(1);
                        var creationTime = Epoch.AddSeconds(reader.GetInt64(2));

                        Log.Debug("Successfully got pending verification from database.");
                        return new PendingVerification(token, encryptedUsername, discordId, creationTime);
                    }

                    Log.Debug("Failed to get pending verification from database, token does not exist.");
                    throw new RecordNotFoundException("No pending verification with provided token was found.");
                }
            }
        }

        public async Task<List<PendingVerification>> GetAllUserPendingVerifications(ulong user)
        {
            Log.Debug("Getting pending verifications from database for user {user}", user);
            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                cmd.Connection = con;

                cmd.CommandText = "SELECT `token`, `encryptedUsername`, `discordSnowflake`, `creationTime` " +
                                  $"FROM `{PendingVerificationsTable}` WHERE `discordSnowflake` = @user;";
                cmd.Parameters.AddWithValue("@user", user);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    var pendingVerifications = new List<PendingVerification>();
                    while (await reader.ReadAsync())
                    {
                        var token = reader.GetString(0);
                        var length = (int)reader.GetBytes(1, 0, null, 0, 0);
                        var encryptedUsername = new byte[length];
                        var index = 0;

                        while (index < length)
                        {
                            var bytesRead = (int)reader.GetBytes(1, index,
                                encryptedUsername, index, length - index);
                            index += bytesRead;
                        }

                        var discordId = reader.GetUInt64(2);
                        var creationTime = Epoch.AddSeconds(reader.GetInt64(3));

                        pendingVerifications.Add(new PendingVerification(token, encryptedUsername, discordId,
                            creationTime));
                    }

                    Log.Debug("Successfully got pending verification from database.");

                    return pendingVerifications;
                }
            }
        }

        /// <summary>
        ///     Delete matching verification token
        /// </summary>
        public async Task DeleteCodeAsync(string token)
        {
            Log.Debug("Deleting pending verification record from database using token.");
            var rowsAffected = 0;
            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                cmd.Connection = con;

                cmd.CommandText = $"DELETE FROM `{PendingVerificationsTable}` WHERE `token` = @token;";
                cmd.Parameters.AddWithValue("@token", token);

                rowsAffected = await cmd.ExecuteNonQueryAsync();
            }

            Log.Debug(
                "Successfuly deleted pending verification record from database using token. Rows affected: {rowsAffected}",
                rowsAffected);
        }

        /// <summary>
        ///     Delete all verification tokens associated with a Discord ID
        /// </summary>
        public async Task DeleteCodeAsync(ulong discordId)
        {
            Log.Debug("Deleting pending verification records from database using Discord ID {discordId}", discordId);
            var rowsAffected = 0;
            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                cmd.Connection = con;

                cmd.CommandText = $"DELETE FROM `{PendingVerificationsTable}` WHERE `discordSnowflake` = @discordId;";
                cmd.Parameters.AddWithValue("@discordId", discordId);

                rowsAffected = await cmd.ExecuteNonQueryAsync();
            }

            Log.Debug(
                "Successfuly deleted pending verification records from database using Discord ID {discordId}. Rows affected: {rowsAffected}",
                discordId, rowsAffected);
        }

        /// <summary>
        ///     Adds a historic record of username verification for an account.
        /// </summary>
        public async Task AddHistoryAsync(byte[] encryptedUsername, ulong discordId)
        {
            Log.Debug("Adding verification history record to database for Discord ID {discordId}", discordId);
            var rowsAffected = 0;
            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                cmd.Connection = con;

                cmd.CommandText = $"INSERT INTO `{VerificationHistoryTable}` " +
                                  "(`discordSnowflake`, `encryptedUsername`, `verificationTime`) " +
                                  "VALUES (@discordId, @encryptedUsername, @time);";
                cmd.Prepare();

                cmd.Parameters.AddWithValue("@discordId", discordId);
                cmd.Parameters.AddWithValue("@encryptedUsername", encryptedUsername);

                var t = DateTime.UtcNow - Epoch;
                cmd.Parameters.AddWithValue("@time", (long)t.TotalSeconds);

                rowsAffected = await cmd.ExecuteNonQueryAsync();
            }

            Log.Debug(
                "Successfuly added verification history record to database using Discord ID {discordId}. Rows affected: {rowsAffected}",
                discordId, rowsAffected);
        }

        public async Task AddVerificationOverride(ulong discordId, OverrideType type)
        {
            Log.Debug("Adding verification override record to database for Discord ID {discordId}", discordId);
            var rowsAffected = 0;
            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                cmd.Connection = con;

                cmd.CommandText = $"INSERT IGNORE INTO `{VerificationOverrideTable}` " +
                                  "(`discordSnowflake`, `objectType`) " +
                                  "VALUES (@discordId, @type);";
                cmd.Prepare();

                cmd.Parameters.AddWithValue("@discordId", discordId);
                cmd.Parameters.AddWithValue("@type", type.ToString());

                rowsAffected = await cmd.ExecuteNonQueryAsync();
            }

            Log.Debug(
                "Successfuly added verification override record to database using Discord ID {discordId}. Rows affected: {rowsAffected}",
                discordId, rowsAffected);
        }

        public async Task<bool> DeleteVerificationOverrideAsync(ulong discordId)
        {
            Log.Debug("Deleting verification override records from database using Discord ID {discordId}", discordId);
            var rowsAffected = 0;
            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                cmd.Connection = con;

                cmd.CommandText = $"DELETE FROM `{VerificationOverrideTable}` WHERE `discordSnowflake` = @discordId;";
                cmd.Parameters.AddWithValue("@discordId", discordId);

                rowsAffected = await cmd.ExecuteNonQueryAsync();
            }

            Log.Debug(
                "Successfuly deleted verification override record from database using Discord ID {discordId}. Rows affected: {rowsAffected}",
                discordId, rowsAffected);

            return rowsAffected >= 1;
        }

        public async Task<int> GetVerifiedUsersCount()
        {
            Log.Debug("Getting all verified users from database.");

            var users = new List<ulong>();
            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                cmd.Connection = con;

                cmd.CommandText = $"SELECT COUNT(*) FROM `{UsersTable}` WHERE `encryptedUsername` IS NOT NULL;";

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    await reader.ReadAsync();
                    return reader.GetInt32(0);
                }
            }
        }

        public async Task<Dictionary<ulong, OverrideType>> GetAllVerificationOverrides()
        {
            Log.Debug("Getting verification override records from database.");
            var overrides = new Dictionary<ulong, OverrideType>();
            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                cmd.Connection = con;

                cmd.CommandText = "SELECT `discordSnowflake`, `objectType`" +
                                  $"FROM `{VerificationOverrideTable}`;";

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                        try
                        {
                            overrides.Add(reader.GetUInt64(0),
                                (OverrideType)Enum.Parse(typeof(OverrideType), reader.GetString(1)));
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Failed to parse verification override from database. {message}", ex.Message);
                        }
                }
            }

            Log.Debug("Successfuly got verification override records from database.");
            return overrides;
        }

        public async Task<List<ulong>> GetAllVerificationOverrides(OverrideType type)
        {
            Log.Debug("Getting verification override records from database.");
            var overrides = new List<ulong>();
            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                cmd.Connection = con;

                cmd.CommandText = "SELECT `discordSnowflake`" +
                                  $"FROM `{VerificationOverrideTable}` WHERE `objectType` = @type;";

                cmd.Parameters.AddWithValue("@type", type.ToString());

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync()) overrides.Add(reader.GetUInt64(0));
                }
            }

            Log.Debug("Successfuly got verification override records from database.");
            return overrides;
        }

        public async Task Cleanup()
        {
            Log.Debug("Deleting old pending verification records from database.");
            var rowsAffected = 0;
            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                cmd.Connection = con;

                var t = DateTime.UtcNow - Epoch - PendingVerificationDeletionTime;

                cmd.CommandText = $"DELETE FROM `{PendingVerificationsTable}` WHERE `creationTime` < @time;";
                cmd.Parameters.AddWithValue("@time", t.TotalSeconds);

                rowsAffected = await cmd.ExecuteNonQueryAsync();
            }

            Log.Debug(
                "Successfuly deleted old pending verification records from database. Rows affected: {rowsAffected}",
                rowsAffected);
        }

        public async Task<List<VerificationRecord>> GetUserVerificationHistoryAsync(ulong user)
        {
            Log.Debug("Getting verification history records for user {user} from database.", user);
            var records = new List<VerificationRecord>();

            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                cmd.Connection = con;

                cmd.CommandText = "SELECT `discordSnowflake`, `encryptedUsername`, `verificationTime`" +
                                  $"FROM `{VerificationHistoryTable}` WHERE `discordSnowflake` = @user;";

                cmd.Parameters.AddWithValue("@user", user);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var length = (int)reader.GetBytes(1, 0, null, 0, 0);
                        var encryptedUsername = new byte[length];
                        var index = 0;

                        while (index < length)
                        {
                            var bytesRead = (int)reader.GetBytes(1, index,
                                encryptedUsername, index, length - index);
                            index += bytesRead;
                        }

                        records.Add(new VerificationRecord(
                            reader.GetUInt64(0),
                            encryptedUsername,
                            DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(2)).DateTime
                        ));
                    }
                }
            }

            Log.Debug("Successfuly got verification history records from database.");
            return records;
        }


        public struct PendingVerification
        {
            public readonly string Token;
            public readonly byte[] EncryptedUsername;
            public readonly ulong DiscordId;
            public readonly DateTime CreationTime;

            public PendingVerification(string token, byte[] encryptedUsername, ulong discordId, DateTime creationTime)
            {
                Token = token;
                EncryptedUsername = encryptedUsername;
                DiscordId = discordId;
                CreationTime = creationTime;
            }
        }

        public struct VerificationRecord
        {
            public readonly ulong DiscordId;
            public readonly byte[] EncryptedUsername;
            public readonly DateTime VerificationTime;

            public VerificationRecord(ulong discordId, byte[] encryptedUsername, DateTime verificationTime)
            {
                DiscordId = discordId;
                EncryptedUsername = encryptedUsername;
                VerificationTime = verificationTime;
            }
        }
    }

    public class UserStorage
    {
        private const string UsersTable = "users";
        private const string UserCoursesTable = "usercourses";
        private readonly StorageService _storageService;

        public UserStorage(StorageService storageService)
        {
            _storageService = storageService;
        }


        public async Task<bool> IsDisallowCourseJoinSetAsync(ulong discordId)
        {
            Log.Debug("Getting disallow course join status for Discord ID {discordId}", discordId);
            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                cmd.Connection = con;

                cmd.CommandText =
                    $"SELECT `disallowCourseJoin` FROM `{UsersTable}` WHERE `discordSnowflake` = @discordId;";
                cmd.Prepare();
                cmd.Parameters.AddWithValue("@discordId", discordId);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync()) // Read one row
                    {
                        if (await reader.IsDBNullAsync(0))
                            return false;

                        Log.Debug("Successfully got disallow course join status for  Discord ID {discordId}",
                            discordId);
                        return reader.GetByte(0) > 0;
                    }

                    Log.Debug("Failed to get disallow course join status for Discord ID {discordId} user not found.",
                        discordId);
                    return false; // If no record for the user, they have not been disallowed.
                }
            }
        }

        public async Task<List<ulong>> GetAllDisallowedUsersAsync()
        {
            Log.Debug("Getting list of users disallowed from joining courses.");

            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                cmd.Connection = con;

                cmd.CommandText = $"SELECT `discordSnowflake` FROM `{UsersTable}` WHERE `disallowCourseJoin` = 1;";
                cmd.Prepare();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    var users = new List<ulong>();
                    while (await reader.ReadAsync()) users.Add(reader.GetUInt64(0));
                    return users;
                }
            }
        }

        public async Task AllowUserCourseJoinAsync(ulong discordId, bool allowed)
        {
            Log.Debug("Setting user course join disallow flag: {id}", discordId);

            var rowsAffected = 0;
            using (var con = _storageService.GetMySqlConnection())
            {
                await con.OpenAsync();
                await CreateUserIfNotExistAsync(discordId, con);
                using (var cmd = new MySqlCommand())
                {
                    cmd.Connection = con;

                    cmd.CommandText =
                        $"UPDATE `{UsersTable}` SET `disallowCourseJoin` = @allowed WHERE `discordSnowflake` = @discordId;";
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@discordId", discordId);
                    cmd.Parameters.AddWithValue("@allowed", allowed ? 0 : 1);

                    rowsAffected = await cmd.ExecuteNonQueryAsync();
                }
            }

            Log.Debug(
                "Successfully set join disallowed status for Discord ID {discordId}. Rows affected: {rowsAffected}",
                discordId, rowsAffected);
        }

        public async Task<byte[]> GetEncryptedUsernameAsync(ulong discordId)
        {
            Log.Debug("Getting encrypted username for Discord ID {discordId}", discordId);
            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                cmd.Connection = con;

                cmd.CommandText =
                    $"SELECT `encryptedUsername` FROM `{UsersTable}` WHERE `discordSnowflake` = @discordId;";
                cmd.Prepare();
                cmd.Parameters.AddWithValue("@discordId", discordId);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync()) // Read one row
                    {
                        if (await reader.IsDBNullAsync(0))
                            return null;

                        var length = (int)reader.GetBytes(0, 0, null, 0, 0);
                        var encryptedUsername = new byte[length];
                        var index = 0;

                        while (index < length)
                        {
                            var bytesRead = (int)reader.GetBytes(0, index,
                                encryptedUsername, index, length - index);
                            index += bytesRead;
                        }

                        Log.Debug("Successfully got encrypted username for Discord ID {discordId}", discordId);
                        return encryptedUsername;
                    }

                    Log.Debug("Failed to get encrypted username for Discord ID {discordId} user not found.", discordId);
                    throw new RecordNotFoundException($"No user with discordId \"{discordId}\" was found.");
                }
            }
        }

        public async Task SetEncryptedUsernameAsync(ulong discordId, byte[] encryptedUsername)
        {
            Log.Debug("Setting encrypted username for Discord ID {discordId}", discordId);
            var rowsAffected = 0;
            using (var con = _storageService.GetMySqlConnection())
            {
                await con.OpenAsync();
                await CreateUserIfNotExistAsync(discordId, con);
                using (var cmd = new MySqlCommand())
                {
                    cmd.Connection = con;

                    cmd.CommandText =
                        $"UPDATE `{UsersTable}` SET `encryptedUsername` = @encryptedUsername WHERE `discordSnowflake` = @discordId;";
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@discordId", discordId);
                    cmd.Parameters.AddWithValue("@encryptedUsername", encryptedUsername);

                    rowsAffected = await cmd.ExecuteNonQueryAsync();
                }
            }

            Log.Debug("Successfully set encrypted username for Discord ID {discordId}. Rows affected: {rowsAffected}",
                discordId, rowsAffected);
        }

        public async Task EnrollUserAsync(ulong discordId, string courseName)
        {
            Log.Debug("Adding enrollment record into database for user {user} into course {course}", discordId,
                courseName);
            var rowsAffected = 0;
            using (var con = _storageService.GetMySqlConnection())
            {
                await con.OpenAsync();
                await CreateUserIfNotExistAsync(discordId, con);
                using (var cmd = new MySqlCommand())
                {
                    cmd.Connection = con;

                    cmd.CommandText =
                        $"INSERT IGNORE INTO `{UserCoursesTable}` (`userDiscordSnowflake`, `courseName`) VALUES (@userId, @courseName);";
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@userId", discordId);
                    cmd.Parameters.AddWithValue("@courseName", courseName);

                    rowsAffected = await cmd.ExecuteNonQueryAsync();
                }
            }

            Log.Debug(
                "Successfully added enrollment record for user {discordId} course {course}. Rows affected: {rowsAffected}",
                discordId, courseName, rowsAffected);
        }

        public async Task DisenrollUserAsync(ulong discordId, string courseName)
        {
            Log.Debug("Deleting enrollment record into database for user {user} into course {course}", discordId,
                courseName);
            var rowsAffected = 0;
            using (var con = _storageService.GetMySqlConnection())
            {
                await con.OpenAsync();
                await CreateUserIfNotExistAsync(discordId, con);
                using (var cmd = new MySqlCommand())
                {
                    cmd.Connection = con;

                    cmd.CommandText =
                        $"DELETE FROM `{UserCoursesTable}` WHERE `userDiscordSnowflake` = @userId AND `courseName` = @courseName;";
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@userId", discordId);
                    cmd.Parameters.AddWithValue("@courseName", courseName);

                    rowsAffected = await cmd.ExecuteNonQueryAsync();
                }
            }

            Log.Debug(
                "Successfully deleted enrollment record for user {discordId} course {course}. Rows affected: {rowsAffected}",
                discordId, courseName, rowsAffected);
        }

        public async Task<bool> IsUserInCourseAsync(ulong discordId, string courseName)
        {
            Log.Debug("Checking enrollment record from database for user {user} course {course}", discordId,
                courseName);
            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                cmd.Connection = con;

                cmd.CommandText =
                    $"SELECT * FROM `{UserCoursesTable}` WHERE `userDiscordSnowflake` = @userId AND `courseName` = @courseName;";
                cmd.Prepare();
                cmd.Parameters.AddWithValue("@userId", discordId);
                cmd.Parameters.AddWithValue("@courseName", courseName);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    return await reader.ReadAsync();
                }
            }
        }

        public async Task<List<string>> GetUserCoursesAsync(ulong discordId)
        {
            Log.Debug("Getting courses from database for user {user}", discordId);
            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                cmd.Connection = con;

                cmd.CommandText =
                    $"SELECT `courseName` FROM `{UserCoursesTable}` WHERE `userDiscordSnowflake` = @userId;";
                cmd.Prepare();
                cmd.Parameters.AddWithValue("@userId", discordId);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    var courses = new List<string>();
                    while (await reader.ReadAsync()) courses.Add(reader.GetString(0));
                    return courses;
                }
            }
        }

        public async Task CreateUserIfNotExistAsync(ulong discordId)
        {
            using (var con = _storageService.GetMySqlConnection())
            {
                await con.OpenAsync();
                await CreateUserIfNotExistAsync(discordId, con);
            }
        }

        public async Task CreateUserIfNotExistAsync(ulong discordId, MySqlConnection con)
        {
            Log.Debug("Creating Discord user if not exist {discordId}", discordId);
            var rowsAffected = 0;
            using (var cmd = new MySqlCommand())
            {
                cmd.Connection = con;

                cmd.CommandText = $"INSERT IGNORE INTO `{UsersTable}` (`discordSnowflake`) VALUES (@discordId);";
                cmd.Prepare();
                cmd.Parameters.AddWithValue("@discordId", discordId);

                rowsAffected = await cmd.ExecuteNonQueryAsync();
            }

            if (rowsAffected == 0)
                Log.Debug("Creating Discord user skipped, user already exists {discordId}. Rows affected: 0",
                    discordId);
            else
                Log.Information("New Discord user created {discordUser}.", discordId);
        }
    }

    public class CourseStorage
    {
        private const string CategoryTable = "coursecategories";
        private const string CourseTable = "courses";
        private const string UserCoursesTable = "usercourses";
        private const string AutoCreatePatternTable = "autocreatepatterns";
        private const string AliasTable = "coursealiases";

        private readonly StorageService _storageService;

        public CourseStorage(StorageService storageService)
        {
            _storageService = storageService;
        }

        public async Task CreateCategoryAsync(ulong discordId, string autoImportPattern, int autoImportPriority)
        {
            Log.Debug("Adding course category to database for {id}", discordId);
            var rowsAffected = 0;
            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                cmd.Connection = con;

                cmd.CommandText = $"INSERT INTO `{CategoryTable}` " +
                                  "(`discordSnowflake`, `autoImportPattern`, `autoImportPriority`) " +
                                  "VALUES (@discordId, @autoImportPattern, @autoImportPriority);";
                cmd.Prepare();

                cmd.Parameters.AddWithValue("@discordId", discordId);
                cmd.Parameters.AddWithValue("@autoImportPattern", autoImportPattern);
                cmd.Parameters.AddWithValue("@autoImportPriority", autoImportPriority);

                rowsAffected = await cmd.ExecuteNonQueryAsync();
                Log.Debug("Successfully added course category to database for {id}. Rows affected: {rowsAffected}",
                    discordId, rowsAffected);
            }
        }

        public async Task DeleteCategoryAsync(ulong discordId)
        {
            Log.Debug("Deleting course category from database for {id}", discordId);
            var rowsAffected = 0;
            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                cmd.Connection = con;

                cmd.CommandText = $"DELETE FROM `{CategoryTable}` WHERE `discordSnowflake` = @discordId;";
                cmd.Parameters.AddWithValue("@discordId", discordId);
                cmd.Prepare();

                rowsAffected = await cmd.ExecuteNonQueryAsync();
                Log.Debug("Successfully deleted course category from database for {id}. Rows affected: {rowsAffected}",
                    discordId, rowsAffected);
            }
        }

        public async Task<bool> DoesCategoryExistAsync(ulong discordId)
        {
            Log.Debug("Checking existance of course category from database for {id}", discordId);

            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                cmd.Connection = con;

                cmd.CommandText = $"SELECT * FROM `{CategoryTable}` WHERE `discordSnowflake` = @discordId;";
                cmd.Parameters.AddWithValue("@discordId", discordId);
                cmd.Prepare();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    return await reader.ReadAsync();
                }
            }
        }

        public async Task<IList<Category>> GetCategoriesAsync()
        {
            Log.Debug("Getting all categories from database.");

            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                cmd.Connection = con;

                cmd.CommandText =
                    $"SELECT `discordSnowflake`,`autoImportPattern`,`autoImportPriority` FROM `{CategoryTable}`;";
                cmd.Prepare();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    var categories = new List<Category>();
                    while (await reader.ReadAsync())
                        if (await reader.IsDBNullAsync(1))
                            categories.Add(new Category(reader.GetUInt64(0), null, reader.GetInt32(2)));
                        else
                            categories.Add(new Category(reader.GetUInt64(0), reader.GetString(1), reader.GetInt32(2)));
                    return categories;
                }
            }
        }

        public async Task CreateCourseAsync(string name, ulong discordId)
        {
            Log.Debug("Adding course to database for {name} {discordId}", name, discordId);
            var rowsAffected = 0;
            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                cmd.Connection = con;

                cmd.CommandText = $"INSERT IGNORE INTO `{CourseTable}` " +
                                  "(`name`, `discordChannelSnowflake`) " +
                                  "VALUES (@name, @discordId);";
                cmd.Prepare();

                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@discordId", discordId);

                rowsAffected = await cmd.ExecuteNonQueryAsync();
                Log.Debug("Successfully added course to database for {name} {discordId}. Rows affected: {rowsAffected}",
                    name, discordId, rowsAffected);
            }
        }

        public async Task DeleteCourseAsync(string name)
        {
            Log.Debug("Deleting course from database for {name}", name);
            var rowsAffected = 0;
            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                cmd.Connection = con;

                cmd.CommandText = $"DELETE FROM `{CourseTable}` WHERE `name` = @name;";
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Prepare();

                rowsAffected = await cmd.ExecuteNonQueryAsync();
                Log.Debug("Successfully deleted course from database for {name}. Rows affected: {rowsAffected}", name,
                    rowsAffected);
            }
        }

        public async Task DeleteCourseAsync(ulong discordId)
        {
            Log.Debug("Deleting course from database for {id}", discordId);
            var rowsAffected = 0;
            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                cmd.Connection = con;

                cmd.CommandText = $"DELETE FROM `{CourseTable}` WHERE `discordChannelSnowflake` = @discordId;";
                cmd.Parameters.AddWithValue("@discordId", discordId);
                cmd.Prepare();

                rowsAffected = await cmd.ExecuteNonQueryAsync();
                Log.Debug("Successfully deleted course from database for {id}. Rows affected: {rowsAffected}",
                    discordId, rowsAffected);
            }
        }

        public async Task<bool> DoesCourseExistAsync(string name)
        {
            Log.Debug("Checking existance of course from database for {name}", name);

            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                cmd.Connection = con;

                cmd.CommandText = $"SELECT * FROM `{CourseTable}` WHERE `name` = @name;";
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Prepare();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    return await reader.ReadAsync();
                }
            }
        }

        public async Task<bool> DoesCourseExistAsync(ulong discordId)
        {
            Log.Debug("Checking existance of course from database for {id}", discordId);

            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                cmd.Connection = con;

                cmd.CommandText = $"SELECT * FROM `{CourseTable}` WHERE `discordChannelSnowflake` = @discordId;";
                cmd.Parameters.AddWithValue("@discordId", discordId);
                cmd.Prepare();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    return await reader.ReadAsync();
                }
            }
        }

        public async Task<ulong> GetCourseDiscordIdAsync(string name)
        {
            Log.Debug("Getting Discord ID of course from database for {name}", name);

            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                cmd.Connection = con;

                cmd.CommandText = $"SELECT `discordChannelSnowflake` FROM `{CourseTable}` WHERE `name` = @name;";
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Prepare();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync()) return reader.GetUInt64(0);
                    return 0;
                }
            }
        }

        public async Task<string> GetCourseNameAsync(ulong discordId)
        {
            Log.Debug("Getting name of course from database for channel {discordChannel}", discordId);

            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                cmd.Connection = con;

                cmd.CommandText = $"SELECT `name` FROM `{CourseTable}` WHERE `discordChannelSnowflake` = @discordId;";
                cmd.Parameters.AddWithValue("@discordId", discordId);
                cmd.Prepare();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync()) return reader.GetString(0);
                    return string.Empty;
                }
            }
        }

        public async Task<Dictionary<string, ulong>> GetAllCoursesAsync()
        {
            Log.Debug("Getting all courses from database for");

            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                cmd.Connection = con;

                cmd.CommandText = $"SELECT * FROM `{CourseTable}`;";
                cmd.Prepare();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    var courses = new Dictionary<string, ulong>();
                    while (await reader.ReadAsync()) courses.Add(reader.GetString(0), reader.GetUInt64(1));
                    return courses;
                }
            }
        }

        public async Task<List<ulong>> GetCourseUsersAsync(string courseName)
        {
            Log.Debug("Getting users from database for course {course}", courseName);
            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                cmd.Connection = con;

                cmd.CommandText =
                    $"SELECT `userDiscordSnowflake` FROM `{UserCoursesTable}` WHERE `courseName` = @course;";
                cmd.Prepare();
                cmd.Parameters.AddWithValue("@course", courseName);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    var users = new List<ulong>();
                    while (await reader.ReadAsync()) users.Add(reader.GetUInt64(0));
                    return users;
                }
            }
        }

        public async Task AddCourseUsersAsync(string courseName, IList<ulong> users)
        {
            Log.Debug("Setting users in database for course {course}", courseName);
            await using var con = _storageService.GetMySqlConnection();
            await con.OpenAsync();
            await using var transaction =  await con.BeginTransactionAsync();
            using (var cmd = new MySqlCommand())
            {
                cmd.Connection = con;
                cmd.Transaction = transaction;
                cmd.CommandText =
                    $"INSERT INTO `{UserCoursesTable}` (`userDiscordSnowflake`, `courseName`) VALUES (@user, @course);";
                await cmd.PrepareAsync();
                cmd.Parameters.AddWithValue("@course", courseName);
                cmd.Parameters.Add("@user", MySqlDbType.UInt64);

                int rowsAdded = 0;
                foreach (ulong user in users)
                {
                    cmd.Parameters["@user"].Value = user;
                    rowsAdded += await cmd.ExecuteNonQueryAsync();
                }
                
                Log.Debug("Successfully added users to course {courseName}. Rows affected: {rowsAffected}",
                    courseName, rowsAdded);
            }
            await transaction.CommitAsync();
        }
        
        public async Task<List<string>> GetAutoCreatePatternsAsync()
        {
            Log.Debug("Getting join creation prefixes from database");
            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                cmd.Connection = con;

                cmd.CommandText = $"SELECT `pattern` FROM `{AutoCreatePatternTable}`;";
                cmd.Prepare();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    var patterns = new List<string>();
                    while (await reader.ReadAsync()) patterns.Add(reader.GetString(0));
                    return patterns;
                }
            }
        }

        public async Task AddAutoCreatePatternAsync(string pattern)
        {
            Log.Debug("Adding auto create pattern to database {id}", pattern);
            var rowsAffected = 0;
            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                cmd.Connection = con;

                cmd.CommandText = $"INSERT INTO `{AutoCreatePatternTable}` " +
                                  "(`pattern`) " +
                                  "VALUES (@pattern);";
                cmd.Prepare();

                cmd.Parameters.AddWithValue("@pattern", pattern);

                rowsAffected = await cmd.ExecuteNonQueryAsync();
                Log.Debug("Successfully added auto create pattern to database {id}. Rows affected: {rowsAffected}",
                    pattern, rowsAffected);
            }
        }

        public async Task DeleteAutoCreatePatternAsync(string pattern)
        {
            Log.Debug("Deleting auto create pattern from database {pattern}", pattern);
            var rowsAffected = 0;
            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                cmd.Connection = con;

                cmd.CommandText = $"DELETE FROM `{AutoCreatePatternTable}` WHERE `pattern` = @pattern;";
                cmd.Parameters.AddWithValue("@pattern", pattern);
                cmd.Prepare();

                rowsAffected = await cmd.ExecuteNonQueryAsync();
                Log.Debug("Successfully deleted pattern from database {pattern}. Rows affected: {rowsAffected}",
                    pattern, rowsAffected);
            }
        }


        public async Task<List<CourseAlias>> GetAliasesAsync()
        {
            Log.Debug("Getting aliases from database");
            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                cmd.Connection = con;

                cmd.CommandText = $"SELECT `name`,`target`,`hidden` FROM `{AliasTable}`;";
                cmd.Prepare();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    var aliases = new List<CourseAlias>();
                    while (await reader.ReadAsync())
                        aliases.Add(new CourseAlias(
                            reader.GetString(0),
                            reader.GetString(1),
                            reader.GetBoolean(2)
                        ));
                    return aliases;
                }
            }
        }

        public async Task<CourseAlias> GetAliasAsync(string name)
        {
            Log.Debug("Getting aliase from database {name}", name);
            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                cmd.Connection = con;

                cmd.CommandText = $"SELECT `name`,`target`,`hidden` FROM `{AliasTable}` WHERE `name` = @name;";
                cmd.Prepare();

                cmd.Parameters.AddWithValue("@name", name);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                        return new CourseAlias(
                            reader.GetString(0),
                            reader.GetString(1),
                            reader.GetBoolean(2)
                        );
                    return null;
                }
            }
        }

        public async Task AddAliasAsync(string name, string target, bool hidden)
        {
            Log.Debug("Adding alias to database {name}", name);
            var rowsAffected = 0;
            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                cmd.Connection = con;

                cmd.CommandText = $"INSERT INTO `{AliasTable}` " +
                                  "(`name`,`target`,`hidden`) " +
                                  "VALUES (@name,@target,@hidden);";
                cmd.Prepare();

                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@target", target);
                cmd.Parameters.AddWithValue("@hidden", hidden);

                rowsAffected = await cmd.ExecuteNonQueryAsync();
                Log.Debug("Successfully added alias to database {name}. Rows affected: {rowsAffected}", name,
                    rowsAffected);
            }
        }

        public async Task DeleteAliasAsync(string name)
        {
            Log.Debug("Deleting alias from database {name}", name);
            var rowsAffected = 0;
            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                cmd.Connection = con;

                cmd.CommandText = $"DELETE FROM `{AliasTable}` WHERE `name` = @name;";
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Prepare();

                rowsAffected = await cmd.ExecuteNonQueryAsync();
                if (rowsAffected == 0) throw new Exception("Deletion failed, record not found.");
                Log.Debug("Successfully deleted alias from database {name}. Rows affected: {rowsAffected}", name,
                    rowsAffected);
            }
        }

        public class Category
        {
            public readonly string AutoImportPattern;
            public readonly int AutoImportPriority;
            public readonly ulong DiscordId;

            public Category(ulong discordId, string autoImportPattern, int autoImportPriority)
            {
                DiscordId = discordId;
                AutoImportPattern = autoImportPattern;
                AutoImportPriority = autoImportPriority;
            }
        }

        public class CourseAlias
        {
            public readonly bool Hidden;
            public readonly string Name;
            public readonly string Target;

            public CourseAlias(string name, string target, bool hidden)
            {
                Name = name;
                Target = target;
                Hidden = hidden;
            }
        }
    }

    public class ServerMessageStorage
    {
        private const string ServerMessageTable = "servermessages";

        private readonly StorageService _storageService;

        public ServerMessageStorage(StorageService storageService)
        {
            _storageService = storageService;
        }

        public async Task CreateServerMessageAsync(ServerMessage message)
        {
            Log.Debug("Adding/Update server message in database {id}", message.MessageID);
            var rowsAffected = 0;
            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                cmd.Connection = con;

                using (var transaction = await con.BeginTransactionAsync())
                {
                    cmd.Transaction = transaction;

                    // Delete any existing messages, we are in a transaction so it's ok
                    cmd.CommandText = $"DELETE FROM `{ServerMessageTable}` WHERE `messageID` = @messageID;";
                    cmd.Parameters.AddWithValue("@messageID", message.MessageID);
                    cmd.Parameters.AddWithValue("@channelID", message.ChannelID);
                    cmd.Parameters.AddWithValue("@content", message.Content);
                    cmd.Parameters.AddWithValue("@created", message.CreatedAt.ToUnixTimeSeconds());
                    cmd.Parameters.AddWithValue("@creator", message.Creator);
                    cmd.Parameters.AddWithValue("@lastEdited", message.LastEditedAt.ToUnixTimeSeconds());
                    cmd.Parameters.AddWithValue("@editor", message.LastEditor);
                    cmd.Parameters.AddWithValue("@name", message.Name);
                    cmd.Prepare();
                    await cmd.ExecuteNonQueryAsync();

                    cmd.CommandText = $"INSERT INTO `{ServerMessageTable}` " +
                                      "(`messageID`, `channelID`, `content`, `created`, `creator`, `lastEdited`, `editor`, `name`) " +
                                      "VALUES(@messageID, @channelID, @content, @created, @creator, @lastEdited, @editor, @name);";
                    cmd.Prepare();

                    rowsAffected = await cmd.ExecuteNonQueryAsync();
                    await transaction.CommitAsync();
                    Log.Debug("Successfully added server message to database {id}. Rows affected: {rowsAffected}",
                        message.MessageID, rowsAffected);
                }
            }
        }

        public async Task DeleteServerMessageAsync(ulong messageID)
        {
            Log.Debug("Deleting server message from database {id}", messageID);
            var rowsAffected = 0;
            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                cmd.Connection = con;

                cmd.CommandText = $"DELETE FROM `{ServerMessageTable}` WHERE `messageID` = @messageID;";
                cmd.Parameters.AddWithValue("@messageID", messageID);
                cmd.Prepare();

                rowsAffected = await cmd.ExecuteNonQueryAsync();
                Log.Debug("Successfully deleted server message from database {id}. Rows affected: {rowsAffected}",
                    messageID, rowsAffected);
            }
        }

        public async Task<bool> DoesServerMessageExistAsync(ulong messageID)
        {
            Log.Debug("Checking existance of server message from database {id}", messageID);

            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                cmd.Connection = con;

                cmd.CommandText = $"SELECT * FROM `{ServerMessageTable}` WHERE `messageID` = @messageID;";
                cmd.Parameters.AddWithValue("@messageID", messageID);
                cmd.Prepare();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    return await reader.ReadAsync();
                }
            }
        }

        public async Task<ServerMessage> GetServerMessageAsync(ulong messageID)
        {
            Log.Debug("Getting server message from database.");

            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                cmd.Connection = con;

                cmd.CommandText =
                    "SELECT `messageID`, `channelID`, `content`, `created`, `creator`, `lastEdited`, `editor`, `name` " +
                    $"FROM `{ServerMessageTable}` " +
                    "WHERE `messageID` = @messageID;";
                cmd.Parameters.AddWithValue("@messageID", messageID);
                cmd.Prepare();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    var serverMessages = new List<ServerMessage>();
                    if (!await reader.ReadAsync())
                        return null;

                    return new ServerMessage(
                        reader.GetUInt64(0), // messageID
                        reader.GetUInt64(1), // ChannelID
                        reader.GetString(2), // Content
                        DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(3)), // Created
                        reader.GetUInt64(4), // Creator
                        DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(5)), // LastEdited
                        reader.GetUInt64(6), // Editor
                        reader.GetString(7)); // Name
                }
            }
        }

        public async Task<IList<ServerMessage>> GetServerMessagesAsync()
        {
            Log.Debug("Getting all server messages from database.");

            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                cmd.Connection = con;

                cmd.CommandText =
                    $"SELECT `messageID`, `channelID`, `content`, `created`, `creator`, `lastEdited`, `editor`, `name` FROM `{ServerMessageTable}`;";
                cmd.Prepare();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    var serverMessages = new List<ServerMessage>();
                    while (await reader.ReadAsync())
                        serverMessages.Add(new ServerMessage(
                            reader.GetUInt64(0), // messageID
                            reader.GetUInt64(1), // ChannelID
                            reader.GetString(2), // Content
                            DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(3)), // Created
                            reader.GetUInt64(4), // Creator
                            DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(5)), // LastEdited
                            reader.GetUInt64(6), // Editor
                            reader.GetString(7))); // Name
                    return serverMessages;
                }
            }
        }

        public class ServerMessage
        {
            public readonly ulong ChannelID;
            public readonly string Content;
            public readonly DateTimeOffset CreatedAt;
            public readonly ulong Creator;
            public readonly DateTimeOffset LastEditedAt;
            public readonly ulong LastEditor;
            public readonly ulong MessageID;
            public readonly string Name;

            public ServerMessage(ulong messageID, ulong channelID, string content, DateTimeOffset createdAt,
                ulong creator, DateTimeOffset lastEditedAt, ulong lastEditor, string name)
            {
                MessageID = messageID;
                ChannelID = channelID;
                Content = content ?? throw new ArgumentNullException(nameof(content));
                CreatedAt = createdAt;
                Creator = creator;
                LastEditedAt = lastEditedAt;
                LastEditor = lastEditor;
                Name = name ?? throw new ArgumentNullException(nameof(name));
            }
        }
    }
    
     public class MinecraftStorage
    {
        private const string MinecraftAccountsTable = "mcaccounts";
        private readonly StorageService _storageService;

        public MinecraftStorage(StorageService storageService)
        {
            _storageService = storageService;
        }

        public record MinecraftAccount(Guid MinecraftUuid, ulong DiscordId, DateTimeOffset CreationTime, bool IsExternal);

        public async Task<IList<MinecraftAccount>> GetMinecraftAccountsAsync()
        {
            Log.Debug("Getting all Minecraft accounts from database.");

            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                cmd.Connection = con;

                cmd.CommandText =
                    $"SELECT `minecraftUuid`, `discordSnowflake`, `creationTime`, `isExternal` FROM `{MinecraftAccountsTable}`;";
                cmd.Prepare();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    var accounts = new List<MinecraftAccount>();
                    while (await reader.ReadAsync())
                        accounts.Add(new MinecraftAccount(
                            reader.GetGuid(0), // minecraftUUID
                            reader.GetUInt64(1), // discordSnowflake
                            DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(2)), // creationTime
                            reader.GetBoolean(3) // isExternal
                        ));
                    return accounts;
                }
            }
        }

        public async Task<MinecraftAccount> GetMinecraftAccountAsync(Guid minecraftUuid)
        {
            Log.Debug("Getting Minecraft account from database.");

            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                cmd.Connection = con;

                cmd.CommandText =
                    "SELECT `minecraftUuid`, `discordSnowflake`, `creationTime`, `isExternal` " +
                    $"FROM `{MinecraftAccountsTable}` " +
                    "WHERE `minecraftUuid` = @minecraftUuid;";
                cmd.Parameters.AddWithValue("@minecraftUuid", minecraftUuid);
                cmd.Prepare();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (!await reader.ReadAsync())
                        return null;

                    return new MinecraftAccount(
                        reader.GetGuid(0), // minecraftUuid
                        reader.GetUInt64(1), // discordSnowflake
                        DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(2)), // creationTime
                        reader.GetBoolean(3)); // Is external
                }
            }
        }
        
        public async Task<MinecraftAccount> FindMinecraftAccountAsync(ulong discordId, bool isExternal)
        {
            Log.Debug("Getting Minecraft account from database.");

            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                cmd.Connection = con;

                cmd.CommandText =
                    "SELECT `minecraftUuid`, `discordSnowflake`, `creationTime`, `isExternal` " +
                    $"FROM `{MinecraftAccountsTable}` " +
                    "WHERE `discordSnowflake` = @discordSnowflake AND `isExternal` = @isExternal;";
                cmd.Parameters.AddWithValue("@discordSnowflake", discordId);
                cmd.Parameters.AddWithValue("@isExternal", isExternal);
                cmd.Prepare();

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (!await reader.ReadAsync())
                        return null;

                    return new MinecraftAccount(
                        reader.GetGuid(0), // minecraftUuid
                        reader.GetUInt64(1), // discordSnowflake
                        DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(2)), // creationTime
                        reader.GetBoolean(3)); // Is external
                }
            }
        }
        
        public async Task CreateMinecraftAccountAsync(MinecraftAccount account)
        {
            Log.Debug("Adding/Update Minecraft account in database {id}", account.MinecraftUuid);
            var rowsAffected = 0;
            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                await _storageService.Users.CreateUserIfNotExistAsync(account.DiscordId, con);
                cmd.Connection = con;
                
                cmd.CommandText = $"INSERT INTO `{MinecraftAccountsTable}` " +
                                  "(`minecraftUuid`, `discordSnowflake`, `creationTime`, `isExternal`) " +
                                  "VALUES(@minecraftUuid, @discordSnowflake, @creationTime, @isExternal);";
                cmd.Parameters.AddWithValue("@minecraftUuid", account.MinecraftUuid);
                cmd.Parameters.AddWithValue("@discordSnowflake", account.DiscordId);
                cmd.Parameters.AddWithValue("@creationTime", account.CreationTime.ToUnixTimeSeconds());
                cmd.Parameters.AddWithValue("@isExternal", account.IsExternal);
                await cmd.PrepareAsync();

                rowsAffected = await cmd.ExecuteNonQueryAsync();
                Log.Debug("Successfully added Minecraft account to database {id}. Rows affected: {rowsAffected}",
                    account.MinecraftUuid, rowsAffected);
            }
        }

        public async Task<int> DeleteMinecraftAccountAsync(Guid minecraftUuid)
        {
            Log.Debug("Deleting Minecraft account from database {id}", minecraftUuid);
            var rowsAffected = 0;
            using (var con = _storageService.GetMySqlConnection())
            using (var cmd = new MySqlCommand())
            {
                await con.OpenAsync();
                cmd.Connection = con;

                cmd.CommandText = $"DELETE FROM `{MinecraftAccountsTable}` WHERE `minecraftUuid` = @minecraftUuid;";
                cmd.Parameters.AddWithValue("@minecraftUuid", minecraftUuid);
                cmd.Prepare();

                rowsAffected = await cmd.ExecuteNonQueryAsync();
                Log.Debug("Successfully deleted Minecraft account from database {id}. Rows affected: {rowsAffected}",
                    minecraftUuid, rowsAffected);
                return rowsAffected;
            }
        }
    }
}