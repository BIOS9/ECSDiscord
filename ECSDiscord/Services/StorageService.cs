using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ECSDiscord.Services
{
    public class StorageService
    {
        private readonly IConfigurationRoot _config;
        private string _mysqlConnectionString;

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
            private StorageService _storageService;

            public VerificationStorage(StorageService storageService)
            {
                _storageService = storageService;
            }


            public async Task AddVerificationCodeAsync(string token, string username, ulong discordId)
            {
                using (MySqlConnection con = _storageService.GetMySqlConnection())
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    con.Open();
                    cmd.Connection = con;

                    cmd.CommandText = $"INSERT INTO `{PendingVerificationsTable}` " +
                        $"(`token`, `username`, `discordSnowflake`, `creationTime`) " +
                        $"VALUES (@token, @username, @discordId, @time);";
                    cmd.Prepare();

                    cmd.Parameters.AddWithValue("@token", token);
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@discordId", discordId);

                    TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
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


            public async Task<string> GetVerifiedUsernameAsync(ulong discordId)
            {
                using (MySqlConnection con = _storageService.GetMySqlConnection())
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    con.Open();
                    cmd.Connection = con;

                    cmd.CommandText = $"SELECT `verifiedVuwUsername` FROM `users` WHERE `discordSnowflake` = @discordId;";
                    cmd.Prepare();
                    cmd.Parameters.AddWithValue("@discordId", discordId);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if(await reader.ReadAsync()) // Read one row
                        {
                            return reader.GetString(0); // Get string in first column
                        }
                        throw new RecordNotFoundException($"No user with discordId {discordId} was found.");
                    }
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
    
            if(!int.TryParse(_config["database:port"], out int port))
            {
                Log.Error("Invalid port number configured in database settings.");
                throw new ArgumentException("Invalid port number configured in database settings.");
            }

            _mysqlConnectionString = $"SERVER={server};PORT={port};DATABASE={database};UID={username};PASSWORD={password};";
        }
    }
}
