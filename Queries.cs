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

        public static string Maintainenance_DropIdx = "DROP INDEX IF EXISTS @idx_name;";
        public static string Maintainenance_CreateIdx = "CREATE INDEX IF NOT EXISTS @idx_name ON @table(@col);";
        public static string Maintainenance_VacuumDB = "VACUUM;";
    }
}
