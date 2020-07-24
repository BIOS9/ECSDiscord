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
                public readonly byte[] UsernameHash;
                public readonly byte[] UsernameHashSalt;
                public readonly ulong DiscordId;
                public readonly DateTime CreationTime;

                public PendingVerification(string token, byte[] encryptedUsername, byte[] usernameHash, byte[] usernameHashSalt, ulong discordId, DateTime creationTime)
                {
                    Token = token;
                    EncryptedUsername = encryptedUsername;
                    UsernameHash = usernameHash;
                    UsernameHashSalt = usernameHashSalt;
                    DiscordId = discordId;
                    CreationTime = creationTime;
                }
            }

            /// <summary>
            /// Add pending verification token to storage.
            /// </summary>
            public async Task AddCodeAsync(string token, byte[] encryptedUsername, byte[] usernameHash, byte[] usernameHashSalt, ulong discordId)
            {
                using (MySqlConnection con = _storageService.GetMySqlConnection())
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    con.Open();
                    cmd.Connection = con;

                    cmd.CommandText = $"INSERT INTO `{PendingVerificationsTable}` " +
                        $"(`token`, `encryptedUsername`, `usernameHash`, `usernameHashSalt`, `discordSnowflake`, `creationTime`) " +
                        $"VALUES (@token, @encryptedUsername, @usernameHash, @usernameHashSalt, @discordId, @time);";
                    cmd.Prepare();

                    cmd.Parameters.AddWithValue("@token", token);
                    cmd.Parameters.AddWithValue("@encryptedUsername", encryptedUsername);
                    cmd.Parameters.AddWithValue("@usernameHash", usernameHash);
                    cmd.Parameters.AddWithValue("@usernameHashSalt", usernameHashSalt);
                    cmd.Parameters.AddWithValue("@discordId", discordId);

                    TimeSpan t = DateTime.UtcNow - Epoch;
                    cmd.Parameters.AddWithValue("@time", (long)t.TotalSeconds);

                    await cmd.ExecuteNonQueryAsync();
                }
            }

            public async Task<PendingVerification> GetPendingVerificationAsync(string token)
            {
                using (MySqlConnection con = _storageService.GetMySqlConnection())
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    con.Open();
                    cmd.Connection = con;

                    cmd.CommandText = $"SELECT `encryptedUsername`, `usernameHash`, `usernameHashSalt`, `discordSnowflake`, `creationTime` " +
                        $"FROM `{PendingVerificationsTable}` WHERE `token` = @token;";
                    cmd.Parameters.AddWithValue("@token", token);

                    using (var reader = (MySqlDataReader)await cmd.ExecuteReaderAsync())
                    {
                        if(await reader.ReadAsync())
                        {
                            byte[] encryptedUsername;
                            using (MemoryStream ms = new MemoryStream())
                            {
                                byte[] buffer = new byte[EncryptedValueBufferSize];
                                int readSize;
                                while ((readSize = (int)reader.GetBytes(0, 0, buffer, 0, buffer.Length)) > 0)
                                    await ms.WriteAsync(buffer, 0, readSize);
                                encryptedUsername = ms.ToArray();
                            }

                            byte[] usernameHash;
                            using (MemoryStream ms = new MemoryStream())
                            {
                                byte[] buffer = new byte[EncryptedValueBufferSize];
                                int readSize;
                                while ((readSize = (int)reader.GetBytes(1, 0, buffer, 0, buffer.Length)) > 0)
                                    await ms.WriteAsync(buffer, 0, readSize);
                                usernameHash = ms.ToArray();
                            }

                            byte[] usernameHashSalt;
                            using (MemoryStream ms = new MemoryStream())
                            {
                                byte[] buffer = new byte[EncryptedValueBufferSize];
                                int readSize;
                                while ((readSize = (int)reader.GetBytes(2, 0, buffer, 0, buffer.Length)) > 0)
                                    await ms.WriteAsync(buffer, 0, readSize);
                                usernameHashSalt = ms.ToArray();
                            }

                            ulong discordId = reader.GetUInt64(3);
                            DateTime creationTime = Epoch.AddSeconds(reader.GetInt64(4));

                            return new PendingVerification(token, encryptedUsername, usernameHash, usernameHashSalt, discordId, creationTime);
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
                    con.Open();
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
                    con.Open();
                    cmd.Connection = con;

                    cmd.CommandText = $"DELETE FROM `{PendingVerificationsTable}` WHERE `discordSnowflake` = @discordId;";
                    cmd.Parameters.AddWithValue("@discordId", discordId);

                    await cmd.ExecuteNonQueryAsync();
                }
            }

            /// <summary>
            /// Adds a historic record of username verification for an account.
            /// </summary>
            public async Task AddHistoryAsync(byte[] encryptedUsername, byte[] usernameHash, byte[] usernameHashSalt, ulong discordId)
            {
                using (MySqlConnection con = _storageService.GetMySqlConnection())
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    con.Open();
                    cmd.Connection = con;

                    cmd.CommandText = $"INSERT INTO `{VerificationHistoryTable}` " +
                        $"(`discordSnowflake`, `encryptedUsername`, `usernameHash`, `usernameHashSalt`, `verificationTime`) " +
                        $"VALUES (@discordId, @encryptedUsername, @usernameHash, @usernameHashSalt, @time);";
                    cmd.Prepare();

                    cmd.Parameters.AddWithValue("@discordId", discordId);
                    cmd.Parameters.AddWithValue("@encryptedUsername", encryptedUsername);
                    cmd.Parameters.AddWithValue("@usernameHash", usernameHash);
                    cmd.Parameters.AddWithValue("@usernameHashSalt", usernameHashSalt);

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
                    con.Open();
                    cmd.Connection = con;

                    cmd.CommandText = $"SELECT `encryptedUsername` FROM `{UsersTable}` WHERE `discordSnowflake` = @discordId;";
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@discordId", discordId);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync()) // Read one row
                        {
                            using (MemoryStream ms = new MemoryStream())
                            {
                                byte[] buffer = new byte[EncryptedValueBufferSize];
                                int readSize;
                                while((readSize = (int)reader.GetBytes(0, 0, buffer, 0, buffer.Length)) > 0)
                                    await ms.WriteAsync(buffer, 0, readSize);
                                return ms.ToArray();
                            }
                        }
                        throw new RecordNotFoundException($"No user with discordId \"{discordId}\" was found.");
                    }
                }
            }

            public async Task SetVerifiedUsernameAsync(ulong discordId, byte[] encryptedUsername)
            {
                using (MySqlConnection con = _storageService.GetMySqlConnection())
                {
                    await CreateUserIfNotExist(discordId, con);
                    using (MySqlCommand cmd = new MySqlCommand())
                    {
                        con.Open();
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
                    con.Open();
                    cmd.Connection = con;

                    cmd.CommandText = $"INSERT INTO `{UsersTable}` (`discordSnowflake`) VALUES (@discordId);";
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
