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
        private const int EncryptedValueBufferSize = 1024;

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
        }


        public UserStorage Users { get; private set; }
        public class UserStorage
        {
            private const string UsersTable = "users";
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
                    await CreateUserIfNotExist(discordId, con);
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

            public async Task CreateUserIfNotExist(ulong discordId)
            {
                using (MySqlConnection con = _storageService.GetMySqlConnection())
                    await CreateUserIfNotExist(discordId, con);
            }

            public async Task CreateUserIfNotExist(ulong discordId, MySqlConnection con)
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

        public StorageService(IConfigurationRoot config)
        {
            Log.Debug("Storage service loading.");
            _config = config;
            loadConfig();

            Verification = new VerificationStorage(this);
            Users = new UserStorage(this);
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
