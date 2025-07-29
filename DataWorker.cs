using CsvHelper;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserDataGenerator_C_
{
    public static class DataWorker
    {
        const int MAX_USERS_PER_ITERATION = 250_000; // Maximum number of users to write per iteration
        public static void CreateDatabase_IfNotExists(string dbPath)
        {
            bool isExists = File.Exists(dbPath);
            if (!isExists)
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

                if (!result)
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
        private static async Task InsertUserToDB(string dbPath, HashSet<User> users)
        {
            using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                await connection.OpenAsync();

                using (var transaction = connection.BeginTransaction())
                try
                {
                    using (var command = new SQLiteCommand(connection))
                    {
                        command.Transaction = transaction;

                        var sb = new StringBuilder();
                        sb.Append("INSERT INTO Users (TaxID, FirstName, LastName, Email, PhoneNumber, PassNumber, Comment) VALUES ");

                        bool first = true;
                        foreach (var user in users)
                        {
                            if (!first) sb.Append(", ");
                            sb.Append($"({user.TaxID}, '{user.FirstName.Replace("'", "''")}', '{user.LastName.Replace("'", "''")}', " +
                                        $"'{user.Email.Replace("'", "''")}', '{user.PhoneNumber}', " +
                                        $"'{user.PassNumber}', '{user.Comment.Replace("'", "''")}')"
                                        );
                            first = false;
                        }

                        command.CommandText = sb.ToString();
                        await command.ExecuteNonQueryAsync();

                        transaction.Commit();
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Error during database transaction: {ex.Message}");
                    try
                    {
                        transaction.Rollback();
                    }
                    catch (Exception rollbackEx)
                    {
                        Log.Error($"Error during transaction rollback: {rollbackEx.Message}");
                    }
                }
                finally
                {
                    connection.Close();
                    if (users.Count > 1) Log.Information($"Inserted {users.Count} users into the database: {dbPath}.");
                }
            }
        }

        [LogMethod]
        private static async Task WriteDataToCSV(string filePath, HashSet<User> users)
        {
            try
            {
                bool fileExists = File.Exists(filePath);
                bool writeHeader = !fileExists || new FileInfo(filePath).Length == 0;

                if (users.Count > 1) Log.Information($"Writing {users.Count} users to CSV file: {filePath}");

                var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = writeHeader,
                    Delimiter = ";",
                };

                using (var writer = new StreamWriter(filePath, append: true))
                using (var csv = new CsvWriter(writer, config))
                {
                    await csv.WriteRecordsAsync(users);
                }
                if (users.Count > 1) Log.Information($"Data written to CSV file: {filePath}");
            }
            catch (Exception ex)
            {
                Log.Error($"Error during attemp to write data to CSV file. {ex.Message}");
            }
        }

        [LogMethod]
        private static async Task CreateIndexIfNotExists(string dbPath, string indexName, string tableName, string columnName)
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
            Log.Information($"Index {indexName} created successfully if it did not exist.");
        }

        [LogMethod]
        private static async Task DropIndexIfExists(string dbPath, string indexName)
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
            Log.Information($"Index {indexName} dropped successfully if it existed.");
        }

        /// <summary>
        /// Drops and creates indexes for the specified table in the database.
        /// in Tuple<string, string, string> on 1st place should be index name, 2nd - table name, 3rd - column name.
        /// </summary>
        /// <param name="dbPath"></param>
        /// <param name="idxData"></param>
        /// <returns></returns>
        public static async Task DBMaintenance_DropAndCreateIdxForTable(string dbPath, List<Tuple<string, string, string>> idxData)
        {
            foreach (var item in idxData)
            {
                await DropIndexIfExists(dbPath, item.Item1);
                await CreateIndexIfNotExists(dbPath, item.Item1, item.Item2, item.Item3);
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
            if (usersSet.Count > MAX_USERS_PER_ITERATION)
            {
                Log.Warning($"The number of users ({usersSet.Count}) exceeds the maximum allowed per iteration ({MAX_USERS_PER_ITERATION}). " +
                            $"The data will be written in chunks of {MAX_USERS_PER_ITERATION} records per iteration.");

                int startIndex = 0;
                while (startIndex < usersSet.Count)
                {
                    var chunk = usersSet.Skip(startIndex).Take(MAX_USERS_PER_ITERATION).ToHashSet();

                    Log.Information($"Writing chunk of {chunk.Count} users to the output.");

                    await DataWriterAsync(parameters, generatedDataPath, paths, chunk);
                    startIndex += MAX_USERS_PER_ITERATION;
                }
            }
            else
            {
                if (usersSet.Count > 1) Log.Information($"Writing {usersSet.Count} users to the output");
                await DataWriterAsync(parameters, generatedDataPath, paths, usersSet);
            }
        }
        private static async Task DataWriterAsync(StartupParameters parameters, string generatedDataPath, Dictionary<string, string> paths, HashSet<User> usersSet)
        {
            switch (parameters.OutputTo)
            {
                case 0: // Write to CSV
                    await WriteDataToCSV(generatedDataPath + paths["PathToCSV"], usersSet);
                    break;
                case 1: // Write to DB
                    await InsertUserToDB(generatedDataPath + paths["PathToDB"], usersSet);
                    break;
                case 2: // Both options (To CSV and DB)
                    await WriteDataToCSV(generatedDataPath + paths["PathToCSV"], usersSet);
                    await InsertUserToDB(generatedDataPath + paths["PathToDB"], usersSet);
                    break;
                default:
                    break;
            }
            GC.Collect();
        }
    }
}
