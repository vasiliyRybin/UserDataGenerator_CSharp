using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserDataGenerator_C_
{
    public static class Queries
    {
        public static string createTableIfNotExists = @"CREATE TABLE IF NOT EXISTS Users (
                                                        TaxID       INTEGER NOT NULL
                                                                    CONSTRAINT PK_Users PRIMARY KEY AUTOINCREMENT,
                                                        Comment     TEXT,
                                                        Email       TEXT,
                                                        FirstName   TEXT,
                                                        LastName    TEXT,
                                                        PassNumber  TEXT,
                                                        PhoneNumber TEXT
                                                    );";

        public static string isTableExists = "SELECT name FROM sqlite_master WHERE type = 'table' AND name = @tableName;";

        public static string GetDataFromTable = "SELECT @cols FROM @table;";

        public static string GetSomeValueFromSomeTable_ReturnNumberOfRows = "SELECT COUNT(@col) AS Cnt FROM @table WHERE @col = '@value'";
    }
}
