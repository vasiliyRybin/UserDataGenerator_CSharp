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
        public static void CreateDatabase_IfNotExists(string dbPath)
        {
            bool isExists = File.Exists(dbPath);
            if(!isExists)
            {
                SQLiteConnection.CreateFile(dbPath);
                Log.Information($"Database created at {dbPath}");
            }
        }

        public static bool CreateUsersTable_IfNotExists(string dbPath)
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
                        command.ExecuteNonQuery();
                    }
                }
            }
            return result;
        }

        public static List<User> GetUsersFromDB(string dbPath)
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
                        while (reader.Read())
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

        public static int GetDataCountFromTable(string dbPath, string columnName, string tblName, string value)
        {
            int result = -1;

            using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                connection.Open();
                var query = Queries.GetSomeValueFromSomeTable_ReturnNumberOfRows.Replace("@col", columnName)
                                                                                .Replace("@table", tblName);
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@value", value);    
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result = int.Parse(reader[0].ToString());
                        }
                    }
                }
            }

            return result;
        }


        public static void InsertUserToDB(string dbPath, HashSet<User> users)
        {
            using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                connection.Open();
                foreach (var user in users)
                {
                    var query = "INSERT INTO Users (TaxID, FirstName, LastName, Email, PhoneNumber, PassNumber, Comment) " +
                                $"VALUES (@TaxID, @FirstName, @LastName, @Email, @PhoneNumber, @PassNumber, '{user.Comment}')";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@TaxID", user.TaxID);
                        command.Parameters.AddWithValue("@FirstName", user.FirstName);
                        command.Parameters.AddWithValue("@LastName", user.LastName);
                        command.Parameters.AddWithValue("@Email", user.Email);
                        command.Parameters.AddWithValue("@PhoneNumber", user.PhoneNumber);
                        command.Parameters.AddWithValue("@PassNumber", user.PassNumber);
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        public static void WriteDataToCSV(string filePath, HashSet<User> users)
        {
            bool fileExists = File.Exists(filePath);
            bool writeHeader = !fileExists || new FileInfo(filePath).Length == 0;

            var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = writeHeader
            };

            using (var writer = new StreamWriter(filePath, append: true))
            using (var csv = new CsvWriter(writer, config))
            {
                csv.WriteRecords(users);
            }
        }

        public static void CreateIndexIfNotExists(string dbPath, string indexName, string tableName, string columnName)
        {
            var query = Queries.Maintainenance_CreateIdx.Replace("@idx_name", indexName)
                                                        .Replace("@table", tableName)
                                                        .Replace("@col", columnName);
            using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                connection.Open();
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        public static void DropIndexIfExists(string dbPath, string indexName)
        {
            var query = Queries.Maintainenance_DropIdx.Replace("@idx_name", indexName);
            using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                connection.Open();
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }   

        public static void VacuumDatabase(string dbPath)
        {
            using (var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                connection.Open();
                using (var command = new SQLiteCommand(Queries.Maintainenance_VacuumDB, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
            Log.Information("Database vacuumed successfully.");
        }
    }
}
