using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ECSDiscord.Services
{
    public class StorageService
    {
        private readonly IConfigurationRoot _config;
        private string _mysqlConnectionString;
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1);
        private static readonly TimeSpan CleanupPeriod = TimeSpan.FromDays(1);

        public class DuplicateRecordException : Exception
        {
            public DuplicateRecordException(string message, Exception innerException) : base(message, innerException) { }
            public DuplicateRecordException(string message) : base(message) { }
            public DuplicateRecordException() { }
        }
        public class RecordNotFoundException : Exception
        {
            public RecordNotFoundException(string message, Exception innerException) : base(message, innerException) { }
            public RecordNotFoundException(string message) : base(message) { }
            public RecordNotFoundException() { }
        }

        public VerificationStorage Verification { get; private set; }
        public class VerificationStorage
        {
            private const string PendingVerificationsTable = "pendingVerifications";
            private const string VerificationHistoryTable = "verificationHistory";
            private const string VerificationOverrideTable = "verificationOverrides";
            private static readonly TimeSpan PendingVerificationDeletionTime = TimeSpan.FromDays(14);

            private StorageService _storageService;

            public VerificationStorage(StorageService storageService)
            {
                _storageService = storageService;
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

            public enum OverrideType
            {
                USER,
                ROLE
            }

            /// <summary>
            /// Add pending verification token to storage.
            /// </summary>
            public async Task AddPendingVerificationAsync(string token, byte[] encryptedUsername, ulong discordId)
            {
                try
                {
                    Log.Debug("Adding pending verification to database for {id}", discordId);
                    int rowsAffected = 0;
                    using (MySqlConnection con = _storageService.GetMySqlConnection())
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        await con.OpenAsync();
                        cmd.Connection = con;

                        cmd.CommandText = $"INSERT INTO `{PendingVerificationsTable}` " +
                            $"(`token`, `encryptedUsername`, `discordSnowflake`, `creationTime`) " +
                            $"VALUES (@token, @encryptedUsername, @discordId, @time);";
                        cmd.Prepare();

                        cmd.Parameters.AddWithValue("@token", token);
                        cmd.Parameters.AddWithValue("@encryptedUsername", encryptedUsername);
                        cmd.Parameters.AddWithValue("@discordId", discordId);

                        TimeSpan t = DateTime.UtcNow - Epoch;
                        cmd.Parameters.AddWithValue("@time", (long)t.TotalSeconds);

                        rowsAffected = await cmd.ExecuteNonQueryAsync();
                        Log.Debug("Successfully added pending verification to database for {id}. Rows affected: {rowsAffected}", discordId, rowsAffected);
                    }
                }
                catch (MySqlException ex)
                {
                    if (ex.Message.StartsWith("Duplicate entry"))
                        throw new DuplicateRecordException("Duplicate verification code.");
                    else
                        throw ex;
                }
            }

            public async Task<PendingVerification> GetPendingVerificationAsync(string token)
            {
                Log.Debug("Getting pending verification from database.");
                using (MySqlConnection con = _storageService.GetMySqlConnection())
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    await con.OpenAsync();
                    cmd.Connection = con;

                    cmd.CommandText = $"SELECT `encryptedUsername`, `discordSnowflake`, `creationTime` " +
                        $"FROM `{PendingVerificationsTable}` WHERE `token` = @token;";
                    cmd.Parameters.AddWithValue("@token", token);

                    using (var reader = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            int length = (int)reader.GetBytes(0, 0, null, 0, 0);
                            byte[] encryptedUsername = new byte[length];
                            int index = 0;

                            while (index < length)
                            {
                                int bytesRead = (int)reader.GetBytes(0, index,
                                                                encryptedUsername, index, length - index);
                                index += bytesRead;
                            }

                            ulong discordId = reader.GetUInt64(1);
                            DateTime creationTime = Epoch.AddSeconds(reader.GetInt64(2));

                            Log.Debug("Successfully got pending verification from database.");
                            return new PendingVerification(token, encryptedUsername, discordId, creationTime);
                        }

                        Log.Debug("Failed to get pending verification from database, token does not exist.");
                        throw new RecordNotFoundException($"No pending verification with provided token was found.");
                    }
                }
            }

            /// <summary>
            /// Delete matching verification token
            /// </summary>
            public async Task DeleteCodeAsync(string token)
            {
                Log.Debug("Deleting pending verification record from database using token.");
                int rowsAffected = 0;
                using (MySqlConnection con = _storageService.GetMySqlConnection())
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    await con.OpenAsync();
                    cmd.Connection = con;

                    cmd.CommandText = $"DELETE FROM `{PendingVerificationsTable}` WHERE `token` = @token;";
                    cmd.Parameters.AddWithValue("@token", token);

                    rowsAffected = await cmd.ExecuteNonQueryAsync();
                }
                Log.Debug("Successfuly deleted pending verification record from database using token. Rows affected: {rowsAffected}", rowsAffected);
            }

            /// <summary>
            /// Delete all verification tokens associated with a Discord ID
            /// </summary>
            public async Task DeleteCodeAsync(ulong discordId)
            {
                Log.Debug("Deleting pending verification records from database using Discord ID {discordId}", discordId);
                int rowsAffected = 0;
                using (MySqlConnection con = _storageService.GetMySqlConnection())
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    await con.OpenAsync();
                    cmd.Connection = con;

                    cmd.CommandText = $"DELETE FROM `{PendingVerificationsTable}` WHERE `discordSnowflake` = @discordId;";
                    cmd.Parameters.AddWithValue("@discordId", discordId);

                    rowsAffected = await cmd.ExecuteNonQueryAsync();
                }
                Log.Debug("Successfuly deleted pending verification records from database using Discord ID {discordId}. Rows affected: {rowsAffected}", discordId, rowsAffected);
            }

            /// <summary>
            /// Adds a historic record of username verification for an account.
            /// </summary>
            public async Task AddHistoryAsync(byte[] encryptedUsername, ulong discordId)
            {
                Log.Debug("Adding verification history record to database for Discord ID {discordId}", discordId);
                int rowsAffected = 0;
                using (MySqlConnection con = _storageService.GetMySqlConnection())
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    await con.OpenAsync();
                    cmd.Connection = con;

                    cmd.CommandText = $"INSERT INTO `{VerificationHistoryTable}` " +
                        $"(`discordSnowflake`, `encryptedUsername`, `verificationTime`) " +
                        $"VALUES (@discordId, @encryptedUsername, @time);";
                    cmd.Prepare();

                    cmd.Parameters.AddWithValue("@discordId", discordId);
                    cmd.Parameters.AddWithValue("@encryptedUsername", encryptedUsername);

                    TimeSpan t = DateTime.UtcNow - Epoch;
                    cmd.Parameters.AddWithValue("@time", (long)t.TotalSeconds);

                    rowsAffected = await cmd.ExecuteNonQueryAsync();
                }
                Log.Debug("Successfuly added verification history record to database using Discord ID {discordId}. Rows affected: {rowsAffected}", discordId, rowsAffected);
            }

            public async Task AddVerificationOverride(ulong discordId, OverrideType type)
            {
                Log.Debug("Adding verification override record to database for Discord ID {discordId}", discordId);
                int rowsAffected = 0;
                using (MySqlConnection con = _storageService.GetMySqlConnection())
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    await con.OpenAsync();
                    cmd.Connection = con;

                    cmd.CommandText = $"INSERT IGNORE INTO `{VerificationOverrideTable}` " +
                        $"(`discordSnowflake`, `objectType`) " +
                        $"VALUES (@discordId, @type);";
                    cmd.Prepare();

                    cmd.Parameters.AddWithValue("@discordId", discordId);
                    cmd.Parameters.AddWithValue("@type", type.ToString());

                    rowsAffected = await cmd.ExecuteNonQueryAsync();
                }
                Log.Debug("Successfuly added verification override record to database using Discord ID {discordId}. Rows affected: {rowsAffected}", discordId, rowsAffected);
            }

            public async Task<bool> DeleteVerificationOverrideAsync(ulong discordId)
            {
                Log.Debug("Deleting verification override records from database using Discord ID {discordId}", discordId);
                int rowsAffected = 0;
                using (MySqlConnection con = _storageService.GetMySqlConnection())
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    await con.OpenAsync();
                    cmd.Connection = con;

                    cmd.CommandText = $"DELETE FROM `{VerificationOverrideTable}` WHERE `discordSnowflake` = @discordId;";
                    cmd.Parameters.AddWithValue("@discordId", discordId);

                    rowsAffected = await cmd.ExecuteNonQueryAsync();
                }
                Log.Debug("Successfuly deleted verification override record from database using Discord ID {discordId}. Rows affected: {rowsAffected}", discordId, rowsAffected);

                return rowsAffected >= 1;
            }


            public async Task<Dictionary<ulong, OverrideType>> GetAllVerificationOverrides()
            {
                Log.Debug("Getting verification override records from database.");
                Dictionary<ulong, OverrideType> overrides = new Dictionary<ulong, OverrideType>();
                using (MySqlConnection con = _storageService.GetMySqlConnection())
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    await con.OpenAsync();
                    cmd.Connection = con;

                    cmd.CommandText = $"SELECT `discordSnowflake`, `objectType`" +
                        $"FROM `{VerificationOverrideTable}`;";

                    using (var reader = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            try
                            {
                                overrides.Add(reader.GetUInt64(0), (OverrideType)Enum.Parse(typeof(OverrideType), reader.GetString(1)));
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Failed to parse verification override from database. {message}", ex.Message);
                            }
                        }
                    }
                }
                Log.Debug("Successfuly got verification override records from database.");
                return overrides;
            }

            public async Task<List<ulong>> GetAllVerificationOverrides(OverrideType type)
            {
                Log.Debug("Getting verification override records from database.");
                List<ulong> overrides = new List<ulong>();
                using (MySqlConnection con = _storageService.GetMySqlConnection())
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    await con.OpenAsync();
                    cmd.Connection = con;

                    cmd.CommandText = $"SELECT `discordSnowflake`" +
                        $"FROM `{VerificationOverrideTable}` WHERE `objectType` = @type;";

                    cmd.Parameters.AddWithValue("@type", type.ToString());

                    using (var reader = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            overrides.Add(reader.GetUInt64(0));
                        }
                    }
                }
                Log.Debug("Successfuly got verification override records from database.");
                return overrides;
            }

            public async Task Cleanup()
            {
                Log.Debug("Deleting old pending verification records from database.");
                int rowsAffected = 0;
                using (MySqlConnection con = _storageService.GetMySqlConnection())
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    await con.OpenAsync();
                    cmd.Connection = con;

                    TimeSpan t = DateTime.UtcNow - Epoch - PendingVerificationDeletionTime;

                    cmd.CommandText = $"DELETE FROM `{PendingVerificationsTable}` WHERE `creationTime` < @time;";
                    cmd.Parameters.AddWithValue("@time", t.TotalSeconds);

                    rowsAffected = await cmd.ExecuteNonQueryAsync();
                }
                Log.Debug("Successfuly deleted old pending verification records from database. Rows affected: {rowsAffected}", rowsAffected);
            }
        }


        public UserStorage Users { get; private set; }
        public class UserStorage
        {
            private const string UsersTable = "users";
            private const string UserCoursesTable = "userCourses";
            private StorageService _storageService;

            public UserStorage(StorageService storageService)
            {
                _storageService = storageService;
            }


            public async Task<byte[]> GetEncryptedUsernameAsync(ulong discordId)
            {
                Log.Debug("Getting encrypted username for Discord ID {discordId}", discordId);
                using (MySqlConnection con = _storageService.GetMySqlConnection())
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    await con.OpenAsync();
                    cmd.Connection = con;

                    cmd.CommandText = $"SELECT `encryptedUsername` FROM `{UsersTable}` WHERE `discordSnowflake` = @discordId;";
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@discordId", discordId);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync()) // Read one row
                        {

                            if (await reader.IsDBNullAsync(0))
                                return null;

                            int length = (int)reader.GetBytes(0, 0, null, 0, 0);
                            byte[] encryptedUsername = new byte[length];
                            int index = 0;

                            while (index < length)
                            {
                                int bytesRead = (int)reader.GetBytes(0, index,
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
                int rowsAffected = 0;
                using (MySqlConnection con = _storageService.GetMySqlConnection())
                {
                    await con.OpenAsync();
                    await CreateUserIfNotExistAsync(discordId, con);
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = con;

                        cmd.CommandText = $"UPDATE `{UsersTable}` SET `encryptedUsername` = @encryptedUsername WHERE `discordSnowflake` = @discordId;";
                        cmd.Prepare();
                        cmd.Parameters.AddWithValue("@discordId", discordId);
                        cmd.Parameters.AddWithValue("@encryptedUsername", encryptedUsername);

                        rowsAffected = await cmd.ExecuteNonQueryAsync();
                    }
                }
                Log.Debug("Successfully set encrypted username for Discord ID {discordId}. Rows affected: {rowsAffected}", discordId, rowsAffected);
            }

            public async Task EnrollUserAsync(ulong discordId, string courseName)
            {
                Log.Debug("Adding enrollment record into database for user {user} into course {course}", discordId, courseName);
                int rowsAffected = 0;
                using (MySqlConnection con = _storageService.GetMySqlConnection())
                {
                    await con.OpenAsync();
                    await CreateUserIfNotExistAsync(discordId, con);
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = con;

                        cmd.CommandText = $"INSERT IGNORE INTO `{UserCoursesTable}` (`userDiscordSnowflake`, `courseName`) VALUES (@userId, @courseName);";
                        cmd.Prepare();
                        cmd.Parameters.AddWithValue("@userId", discordId);
                        cmd.Parameters.AddWithValue("@courseName", courseName);

                        rowsAffected = await cmd.ExecuteNonQueryAsync();
                    }
                }
                Log.Debug("Successfully added enrollment record for user {discordId} course {course}. Rows affected: {rowsAffected}", discordId, courseName, rowsAffected);
            }

            public async Task DisenrollUserAsync(ulong discordId, string courseName)
            {
                Log.Debug("Deleting enrollment record into database for user {user} into course {course}", discordId, courseName);
                int rowsAffected = 0;
                using (MySqlConnection con = _storageService.GetMySqlConnection())
                {
                    await con.OpenAsync();
                    await CreateUserIfNotExistAsync(discordId, con);
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        cmd.Connection = con;

                        cmd.CommandText = $"DELETE FROM `{UserCoursesTable}` WHERE `userDiscordSnowflake` = @userId AND `courseName` = @courseName;";
                        cmd.Prepare();
                        cmd.Parameters.AddWithValue("@userId", discordId);
                        cmd.Parameters.AddWithValue("@courseName", courseName);

                        rowsAffected = await cmd.ExecuteNonQueryAsync();
                    }
                }
                Log.Debug("Successfully deleted enrollment record for user {discordId} course {course}. Rows affected: {rowsAffected}", discordId, courseName, rowsAffected);
            }

            public async Task<bool> IsUserInCourseAsync(ulong discordId, string courseName)
            {
                Log.Debug("Checking enrollment record from database for user {user} course {course}", discordId, courseName);
                using (MySqlConnection con = _storageService.GetMySqlConnection())
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    await con.OpenAsync();
                    cmd.Connection = con;

                    cmd.CommandText = $"SELECT * FROM `{UserCoursesTable}` WHERE `userDiscordSnowflake` = @userId AND `courseName` = @courseName;";
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@userId", discordId);
                    cmd.Parameters.AddWithValue("@courseName", courseName);

                    using (var reader = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                    {
                        return await reader.ReadAsync();
                    }
                }
            }

            public async Task<List<string>> GetUserCoursesAsync(ulong discordId)
            {
                Log.Debug("Getting courses from database for user {user}", discordId);
                using (MySqlConnection con = _storageService.GetMySqlConnection())
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    await con.OpenAsync();
                    cmd.Connection = con;

                    cmd.CommandText = $"SELECT `courseName` FROM `{UserCoursesTable}` WHERE `userDiscordSnowflake` = @userId;";
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@userId", discordId);

                    using (var reader = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                    {
                        List<string> courses = new List<string>();
                        while(await reader.ReadAsync())
                        {
                            courses.Add(reader.GetString(0));
                        }
                        return courses;
                    }
                }
            }

            public async Task CreateUserIfNotExistAsync(ulong discordId)
            {
                using (MySqlConnection con = _storageService.GetMySqlConnection())
                    await CreateUserIfNotExistAsync(discordId, con);
            }

            public async Task CreateUserIfNotExistAsync(ulong discordId, MySqlConnection con)
            {
                Log.Debug("Creating Discord user if not exist {discordId}", discordId);
                int rowsAffected = 0;
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.Connection = con;

                    cmd.CommandText = $"INSERT IGNORE INTO `{UsersTable}` (`discordSnowflake`) VALUES (@discordId);";
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@discordId", discordId);

                    rowsAffected = await cmd.ExecuteNonQueryAsync();
                }
                if (rowsAffected == 0)
                    Log.Debug("Creating Discord user skipped, user already exists {discordId}. Rows affected: 0", discordId);
                else
                    Log.Information("New Discord user created {discordUser}.", discordId);
            }

            public async Task Cleanup()
            {

            }
        }


        public CourseStorage Courses { get; private set; }
        public class CourseStorage
        {
            private const string CategoryTable = "courseCategories";
            private const string CourseTable = "courses";
            private const string UserCoursesTable = "userCourses";

            private StorageService _storageService;

            public CourseStorage(StorageService storageService)
            {
                _storageService = storageService;
            }

            public async Task CreateCategoryAsync(ulong discordId, string autoImportPattern, int autoImportPriority)
            {
                Log.Debug("Adding course category to database for {id}", discordId);
                int rowsAffected = 0;
                using (MySqlConnection con = _storageService.GetMySqlConnection())
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    await con.OpenAsync();
                    cmd.Connection = con;

                    cmd.CommandText = $"INSERT INTO `{CategoryTable}` " +
                        $"(`discordSnowflake`, `autoImportPattern`, `autoImportPriority`) " +
                        $"VALUES (@discordId, @autoImportPattern, @autoImportPriority);";
                    cmd.Prepare();

                    cmd.Parameters.AddWithValue("@discordId", discordId);
                    cmd.Parameters.AddWithValue("@autoImportPattern", autoImportPattern);
                    cmd.Parameters.AddWithValue("@autoImportPriority", autoImportPriority);

                    rowsAffected = await cmd.ExecuteNonQueryAsync();
                    Log.Debug("Successfully added course category to database for {id}. Rows affected: {rowsAffected}", discordId, rowsAffected);
                }
            }

            public async Task DeleteCategoryAsync(ulong discordId)
            {
                Log.Debug("Deleting course category from database for {id}", discordId);
                int rowsAffected = 0;
                using (MySqlConnection con = _storageService.GetMySqlConnection())
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    await con.OpenAsync();
                    cmd.Connection = con;

                    cmd.CommandText = $"DELETE FROM `{CategoryTable}` WHERE `discordSnowflake` = @discordId;";
                    cmd.Parameters.AddWithValue("@discordId", discordId);
                    cmd.Prepare();

                    rowsAffected = await cmd.ExecuteNonQueryAsync();
                    Log.Debug("Successfully deleted course category from database for {id}. Rows affected: {rowsAffected}", discordId, rowsAffected);
                }
            }

            public async Task<bool> DoesCategoryExistAsync(ulong discordId)
            {
                Log.Debug("Checking existance of course category from database for {id}", discordId);

                using (MySqlConnection con = _storageService.GetMySqlConnection())
                using (MySqlCommand cmd = new MySqlCommand())
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

            public async Task CreateCourseAsync(string name, ulong discordId)
            {
                Log.Debug("Adding course to database for {name} {discordId}", name, discordId);
                int rowsAffected = 0;
                using (MySqlConnection con = _storageService.GetMySqlConnection())
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    await con.OpenAsync();
                    cmd.Connection = con;

                    cmd.CommandText = $"INSERT INTO `{CourseTable}` " +
                        $"(`name`, `discordChannelSnowflake`) " +
                        $"VALUES (@name, @discordId);";
                    cmd.Prepare();

                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@discordId", discordId);

                    rowsAffected = await cmd.ExecuteNonQueryAsync();
                    Log.Debug("Successfully added course to database for {name} {discordId}. Rows affected: {rowsAffected}", name, discordId, rowsAffected);
                }
            }

            public async Task DeleteCourseAsync(string name)
            {
                Log.Debug("Deleting course from database for {name}", name);
                int rowsAffected = 0;
                using (MySqlConnection con = _storageService.GetMySqlConnection())
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    await con.OpenAsync();
                    cmd.Connection = con;

                    cmd.CommandText = $"DELETE FROM `{CourseTable}` WHERE `name` = @name;";
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Prepare();

                    rowsAffected = await cmd.ExecuteNonQueryAsync();
                    Log.Debug("Successfully deleted course from database for {name}. Rows affected: {rowsAffected}", name, rowsAffected);
                }
            }

            public async Task DeleteCourseAsync(ulong discordId)
            {
                Log.Debug("Deleting course from database for {id}", discordId);
                int rowsAffected = 0;
                using (MySqlConnection con = _storageService.GetMySqlConnection())
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    await con.OpenAsync();
                    cmd.Connection = con;

                    cmd.CommandText = $"DELETE FROM `{CourseTable}` WHERE `discordChannelSnowflake` = @discordId;";
                    cmd.Parameters.AddWithValue("@discordId", discordId);
                    cmd.Prepare();

                    rowsAffected = await cmd.ExecuteNonQueryAsync();
                    Log.Debug("Successfully deleted course from database for {id}. Rows affected: {rowsAffected}", discordId, rowsAffected);
                }
            }

            public async Task<bool> DoesCourseExistAsync(string name)
            {
                Log.Debug("Checking existance of course from database for {name}", name);

                using (MySqlConnection con = _storageService.GetMySqlConnection())
                using (MySqlCommand cmd = new MySqlCommand())
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

                using (MySqlConnection con = _storageService.GetMySqlConnection())
                using (MySqlCommand cmd = new MySqlCommand())
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

                using (MySqlConnection con = _storageService.GetMySqlConnection())
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    await con.OpenAsync();
                    cmd.Connection = con;

                    cmd.CommandText = $"SELECT `discordChannelSnowflake` FROM `{CourseTable}` WHERE `name` = @name;";
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Prepare();

                    using (var reader = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return reader.GetUInt64(0);
                        }
                        return 0;
                    }
                }
            }

            public async Task<Dictionary<string, ulong>> GetAllCoursesAsync()
            {
                Log.Debug("Getting all courses from database for");

                using (MySqlConnection con = _storageService.GetMySqlConnection())
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    await con.OpenAsync();
                    cmd.Connection = con;

                    cmd.CommandText = $"SELECT * FROM `{CourseTable}`;";
                    cmd.Prepare();

                    using (var reader = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                    {
                        Dictionary<string, ulong> courses = new Dictionary<string, ulong>();
                        while (await reader.ReadAsync())
                        {
                            courses.Add(reader.GetString(0), reader.GetUInt64(1));
                        }
                        return courses;
                    }
                }
            }

            public async Task<List<ulong>> GetCourseUsersAsync(string courseName)
            {
                Log.Debug("Getting users from database for course {course}", courseName);
                using (MySqlConnection con = _storageService.GetMySqlConnection())
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    await con.OpenAsync();
                    cmd.Connection = con;

                    cmd.CommandText = $"SELECT `userDiscordSnowflake` FROM `{UserCoursesTable}` WHERE `courseName` = @course;";
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@course", courseName);

                    using (var reader = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                    {
                        List<ulong> users = new List<ulong>();
                        while (await reader.ReadAsync())
                        {
                            users.Add(reader.GetUInt64(0));
                        }
                        return users;
                    }
                }
            }

            public async Task Cleanup()
            {

            }
        }


        protected MySqlConnection GetMySqlConnection()
        {
            return new MySqlConnection(_mysqlConnectionString);
        }

        public async Task<bool> TestConnection()
        {
            try
            {
                Log.Debug("Testing MySql connection.");
                using (MySqlConnection con = GetMySqlConnection())
                {
                    await con.OpenAsync();
                    Log.Information("Connected to MySql version: {version}", con.ServerVersion);
                }
                Log.Debug("MySql connection test succeeded.");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to connect to MySql {message}", ex.Message);
                return false;
            }
        }

        public async Task Cleanup()
        {
            try
            {
                Log.Information("Database cleanup executed.");
                await Verification.Cleanup();
                await Users.Cleanup();
                await Courses.Cleanup();
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

        public StorageService(IConfigurationRoot config)
        {
            Log.Debug("Storage service loading.");
            _config = config;
            loadConfig();

            Verification = new VerificationStorage(this);
            Users = new UserStorage(this);
            Courses = new CourseStorage(this);
            startCleanupService();
            Log.Debug("Storage service loaded.");
        }

        private void loadConfig()
        {
            string server = _config["database:server"];
            string database = _config["database:database"];
            string username = _config["database:username"];
            string password = _config["database:password"];

            if (!int.TryParse(_config["database:port"], out int port))
            {
                Log.Error("Invalid port number configured in database settings.");
                throw new ArgumentException("Invalid port number configured in database settings.");
            }

            _mysqlConnectionString = $"SERVER={server};PORT={port};DATABASE={database};UID={username};PASSWORD={password};";
        }
    }
}
