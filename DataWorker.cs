using CsvHelper;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace UserDataGenerator_C_
{
    public static class DataWorker
    {
        public static void CreateDatabase_IfNotExists(string dbPath)
        {
            bool isExists = File.Exists(dbPath);
            if(!isExists)
            {
                SQLiteConnection.CreateFile(dbPath);
                Log.Information($"Database created at {dbPath}");
            }
        }

        [LogMethod]
        public static async Task<bool> CreateUsersTable_IfNotExistsAsync(string dbPath)
        {
            bool result = false;
            using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                connection.Open();

                // Check if the Users table exists
                using (var command = new SQLiteCommand(Queries.isTableExists, connection))
                {
                    command.Parameters.AddWithValue("@tableName", "Users");
                    using (var reader = command.ExecuteReader())
                    {
                        result = reader.HasRows;
                    }
                }

                if(!result)
                {
                    using (var command = new SQLiteCommand(Queries.createTableIfNotExists, connection))
                    {
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            return result;
        }

        [LogMethod]
        public static async Task<List<User>> GetUsersFromDBAsync(string dbPath)
        {
            List<User> users = new List<User>();
            using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                connection.Open();
                var query = Queries.GetDataFromTable.Replace("@cols", "TaxID, FirstName, LastName, Email, PhoneNumber, PassNumber, Comment")
                                                    .Replace("@table", "Users");
                using (var command = new SQLiteCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (await reader.ReadAsync())
                        {
                            User user = new User(
                                int.Parse(reader["TaxID"].ToString()),
                                reader["FirstName"].ToString(),
                                reader["LastName"].ToString(),
                                reader["Email"].ToString(),
                                reader["PhoneNumber"].ToString(),
                                reader["PassNumber"].ToString(),
                                reader["Comment"].ToString()
                            );
                            users.Add(user);
                        }
                    }
                }
            }
            return users;
        }

        [LogMethod]
        public static async Task<int> GetDataCountFromTable(string dbPath, string columnName, string tblName, string value)
        {
            int result = -1;

            using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                connection.Open();
                var query = Queries.GetSomeValueFromSomeTable_ReturnNumberOfRows.Replace("@col", columnName)
                                                                                .Replace("@table", tblName)
                                                                                .Replace("@value", value);
                using (var command = new SQLiteCommand(query, connection))
                {
                    //command.Parameters.AddWithValue("@value", value);    
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            result = int.Parse(reader[0].ToString());
                        }
                    }
                }
            }

            return result;
        }

        [LogMethod]
        public static async Task InsertUserToDB(string dbPath, HashSet<User> users)
        {
            using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                using (var command = new SQLiteCommand(
                    "INSERT INTO Users (TaxID, FirstName, LastName, Email, PhoneNumber, PassNumber, Comment) " +
                    "VALUES (@TaxID, @FirstName, @LastName, @Email, @PhoneNumber, @PassNumber, @Comment)",
                    connection,
                    transaction))
                {
                    var taxIdParam = command.Parameters.Add("@TaxID", System.Data.DbType.Int32);
                    var firstNameParam = command.Parameters.Add("@FirstName", System.Data.DbType.String);
                    var lastNameParam = command.Parameters.Add("@LastName", System.Data.DbType.String);
                    var emailParam = command.Parameters.Add("@Email", System.Data.DbType.String);
                    var phoneNumberParam = command.Parameters.Add("@PhoneNumber", System.Data.DbType.String);
                    var passNumberParam = command.Parameters.Add("@PassNumber", System.Data.DbType.String);
                    var commentParam = command.Parameters.Add("@Comment", System.Data.DbType.String);

                    foreach (var user in users)
                    {
                        try
                        {
                            taxIdParam.Value = user.TaxID;
                            firstNameParam.Value = user.FirstName;
                            lastNameParam.Value = user.LastName;
                            emailParam.Value = user.Email;
                            phoneNumberParam.Value = user.PhoneNumber;
                            passNumberParam.Value = user.PassNumber;
                            commentParam.Value = user.Comment;

                            await command.ExecuteNonQueryAsync();
                        }
                        catch(Exception ex) 
                        { 
                            Log.Error($"Error inserting user: {ex.Message}");
                            Log.Error(user.ToString());
                        }
                    }

                    transaction.Commit();
                }
            }
        }

        [LogMethod]
        public static async Task WriteDataToCSV(string filePath, HashSet<User> users)
        {
            bool fileExists = File.Exists(filePath);
            bool writeHeader = !fileExists || new FileInfo(filePath).Length == 0;

            if(users.Count > 1) Log.Information($"Writing {users.Count} users to CSV file: {filePath}");

            var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = writeHeader
            };

            using (var writer = new StreamWriter(filePath, append: true))
            using (var csv = new CsvWriter(writer, config))
            {
                await csv.WriteRecordsAsync(users);
            }
            if (users.Count > 1) Log.Information($"Data written to CSV file: {filePath}");
        }

        [LogMethod]
        public static async Task CreateIndexIfNotExists(string dbPath, string indexName, string tableName, string columnName)
        {
            var query = Queries.Maintainenance_CreateIdx.Replace("@idx_name", indexName)
                                                        .Replace("@table", tableName)
                                                        .Replace("@col", columnName);
            using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                connection.Open();
                using (var command = new SQLiteCommand(query, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        [LogMethod]
        public static async Task DropIndexIfExists(string dbPath, string indexName)
        {
            var query = Queries.Maintainenance_DropIdx.Replace("@idx_name", indexName);
            using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                connection.Open();
                using (var command = new SQLiteCommand(query, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        [LogMethod]
        public static async Task VacuumDatabase(string dbPath)
        {
            using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                connection.Open();
                using (var command = new SQLiteCommand(Queries.Maintainenance_VacuumDB, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }
            Log.Information("Database vacuumed successfully.");
        }

        [LogMethod]
        public static async Task WriteDataAsync(StartupParameters parameters, string generatedDataPath, Dictionary<string, string> paths, HashSet<User> usersSet)
        {
            var DBPath = generatedDataPath + paths["PathToDB"];

            switch (parameters.OutputTo)
            {
                case 0: // Write to CSV
                    await WriteDataToCSV(generatedDataPath + paths["PathToCSV"], usersSet);
                    break;
                case 1: // Write to DB
                    await InsertUserToDB(DBPath, usersSet);
                    break;
                case 2: // Both options (To CSV and DB)
                    await WriteDataToCSV(generatedDataPath + paths["PathToCSV"], usersSet);
                    await InsertUserToDB(DBPath, usersSet);
                    break;
                default:
                    break;
            }
        }
    }
}
