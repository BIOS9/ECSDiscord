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

                        await cmd.ExecuteNonQueryAsync();
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

                            return new PendingVerification(token, encryptedUsername, discordId, creationTime);
                        }

                        throw new RecordNotFoundException($"No pending verification with token \"{token}\" was found.");
                    }
                }
            }

            /// <summary>
            /// Delete matching verification token
            /// </summary>
            public async Task DeleteCodeAsync(string token)
            {
                using (MySqlConnection con = _storageService.GetMySqlConnection())
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    await con.OpenAsync();
                    cmd.Connection = con;

                    cmd.CommandText = $"DELETE FROM `{PendingVerificationsTable}` WHERE `token` = @token;";
                    cmd.Parameters.AddWithValue("@token", token);

                    await cmd.ExecuteNonQueryAsync();
                }
            }

            /// <summary>
            /// Delete all verification tokens associated with a Discord ID
            /// </summary>
            public async Task DeleteCodeAsync(ulong discordId)
            {
                using (MySqlConnection con = _storageService.GetMySqlConnection())
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    await con.OpenAsync();
                    cmd.Connection = con;

                    cmd.CommandText = $"DELETE FROM `{PendingVerificationsTable}` WHERE `discordSnowflake` = @discordId;";
                    cmd.Parameters.AddWithValue("@discordId", discordId);

                    await cmd.ExecuteNonQueryAsync();
                }
            }

            /// <summary>
            /// Adds a historic record of username verification for an account.
            /// </summary>
            public async Task AddHistoryAsync(byte[] encryptedUsername, ulong discordId)
            {
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

                    await cmd.ExecuteNonQueryAsync();
                }
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

                            return encryptedUsername;
                        }
                        throw new RecordNotFoundException($"No user with discordId \"{discordId}\" was found.");
                    }
                }
            }

            public async Task SetEncryptedUsernameAsync(ulong discordId, byte[] encryptedUsername)
            {
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

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }

            public async Task CreateUserIfNotExist(ulong discordId)
            {
                using (MySqlConnection con = _storageService.GetMySqlConnection())
                    await CreateUserIfNotExist(discordId, con);
            }

            public async Task CreateUserIfNotExist(ulong discordId, MySqlConnection con)
            {
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    cmd.Connection = con;

                    cmd.CommandText = $"INSERT IGNORE INTO `{UsersTable}` (`discordSnowflake`) VALUES (@discordId);";
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@discordId", discordId);

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        protected MySqlConnection GetMySqlConnection()
        {
            return new MySqlConnection(_mysqlConnectionString);
        }

        public StorageService(IConfigurationRoot config)
        {
            _config = config;
            loadConfig();

            Verification = new VerificationStorage(this);
            Users = new UserStorage(this);
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

        public async Task<bool> TestConnection()
        {
            try
            {
                using (MySqlConnection con = GetMySqlConnection())
                {
                    await con.OpenAsync();
                    Log.Information("Connected to MySql version: {version}", con.ServerVersion);
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to connect to MySql {message}", ex.Message);
                return false;
            }
        }
    }
}
