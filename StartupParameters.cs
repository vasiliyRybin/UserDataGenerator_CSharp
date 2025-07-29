using Serilog;

namespace UserDataGenerator_C_
{
    /// <summary>
    /// Available startup parameters for the application as next:
    /// 
    /// amount:100000                   - Any amount of users to be generated, must be greater than 0
    /// invalid_tax_id_ratio:10         - Ratio of invalid tax payer numbers, must be greater than or equal to 0
    /// output_to:2                     - 0 - write to CSV file, 1 - write to DB, 2 - both options (to CSV and DB)
    /// in_memory_processing:0          - 0 - disabled, 1 - enabled (use carefully: loads all data from Users table into RAM. If there's already too much of data, app may crash)
    /// data_bulk_insert:1              - 0 - disabled, 1 - enabled (when disabled, works as next: one user - one insertion. Otherwise all newly generated users 
    ///                                   will be stored in memory and then inserted with one command. But if there're will be more than 250.000 users generated, it will insert 
    ///                                   250.000 per iteration)
    ///                                   
    /// debug                           - if that parameter was added, all the logs will be written to the console and logfile, otherwise only errors and warnings will be logged 
    /// 
    /// 
    /// 
    /// Example of combining startup parameters:
    /// in_memory_processing:1 data_bulk_insert:1  ===> In-memory processing is enabled and DB bulk insert is enabled as well = consuming RAM, but faster processing
    /// in_memory_processing:0 data_bulk_insert:1  ===> consuming RAM only to store data for bulk insert, but not for processing, working speed is slower than previous example
    /// in_memory_processing:1 data_bulk_insert:0  ===> consuming RAM to store data for processing, but not for bulk insert, working speed is almost the same as the previous example
    /// in_memory_processing:0 data_bulk_insert:0  ===> not consuming RAM, working speed is the slowest, but it will not crash if there's too much data in Users table.
    ///                                                 Good for small amounts of data, but not for big ones.
    /// 
    /// </summary>
    public class StartupParameters
    {
        public static readonly int MIN_VALID_TAXES_PAYER_NUMBER = 1;
        public static readonly int MAX_VALID_TAXES_PAYER_NUMBER = int.MaxValue;
        const bool DEFAULT_VALUE_DbBulkInsert = false;
        const bool DEFAULT_VALUE_InMemoryProcessing = false;
        const int DEFAULT_VALUE_Amount = 5000;                  //Default amount of users to be generated

        /*
         * 0 - write to CSV file
         * 1 - Write to DB
         * 2 - Both options (To CSV and DB)
         */
        const int DEFAULT_VALUE_OutputTo = 1;

        /*
         * simulation of "human factor"
         * Higher value - higher risk of invalid tax payer number
         */
        const int DEFAULT_VALUE_InvalidTaxPayerRatio = 10;

        public int InvalidTaxPayerRatio { get; private set; } = DEFAULT_VALUE_InvalidTaxPayerRatio;
        public int Amount { get; private set; } = DEFAULT_VALUE_Amount; // Default amount of users to be generated
        public int OutputTo { get; private set; } = DEFAULT_VALUE_OutputTo;
        public bool InMemoryProcessing { get; private set; } = DEFAULT_VALUE_InMemoryProcessing;
        public bool DbBulkInsert { get; private set; } = DEFAULT_VALUE_DbBulkInsert;

        public StartupParameters(string[] stParams)
        {
            ValidateAndAssignInputParams(stParams);
        }

        private void ValidateAndAssignInputParams(string[] args)
        {
            foreach (var item in args)
            {
                var paramName = item.Split(':')[0].Trim().ToLower();
                switch (paramName)
                {
                    case "amount":
                        if (int.TryParse(item.Split(':')[1].Trim(), out int parsedAmount) && parsedAmount > 0)
                        {
                            Amount = parsedAmount;
                            Log.Information("Amount of users to be generated: {Amount}", Amount);
                        }
                        else Log.Warning("Invalid value for 'amount' parameter. Using default value: {DefaultValue}", DEFAULT_VALUE_Amount);
                        break;

                    case "invalid_tax_id_ratio":
                        if (int.TryParse(item.Split(':')[1].Trim(), out int parsedRatio) && parsedRatio >= 0)
                        {
                            InvalidTaxPayerRatio = parsedRatio;
                            Log.Information("Invalid tax payer number ratio is: {Ratio}", InvalidTaxPayerRatio);
                        }
                        else Log.Warning("Invalid value for 'invalid_tax_id_ratio' parameter. Using default value: {DefaultValue}", DEFAULT_VALUE_InvalidTaxPayerRatio);
                        break;

                    case "output_to":
                        if (int.TryParse(item.Split(':')[1].Trim(), out int parsedOutputTo) && parsedOutputTo >= 0 && parsedOutputTo <= 2)
                        {
                            OutputTo = parsedOutputTo;
                            Log.Information("Output to: {OutputTo}", parsedOutputTo == 0 ? "CSV file" :
                                                                     parsedOutputTo == 1 ? "DB" :
                                                                     "CSV & DB");
                        }
                        else Log.Warning("Invalid value for 'output_to' parameter. Using default value: {DefaultValue}", DEFAULT_VALUE_OutputTo);
                        break;

                    case "in_memory_processing":
                        if (int.TryParse(item.Split(':')[1].Trim(), out int parsedInMemoryProcessing) && (parsedInMemoryProcessing == 0 || parsedInMemoryProcessing == 1))
                        {
                            InMemoryProcessing = parsedInMemoryProcessing == 1;
                            Log.Information("In-memory processing is: {InMemoryProcessing}", InMemoryProcessing ? "Enabled" : "Disabled");
                        }
                        else Log.Warning("Invalid value for 'in_memory_processing' parameter. Using default value: {DefaultValue}", DEFAULT_VALUE_InMemoryProcessing);
                        break;

                    case "data_bulk_insert":
                        if (int.TryParse(item.Split(':')[1].Trim(), out int parsedDbBulkInsert) && (parsedDbBulkInsert == 0 || parsedDbBulkInsert == 1))
                        {
                            DbBulkInsert = parsedDbBulkInsert == 1;
                            Log.Information("DB bulk insert is: {DbBulkInsert}", parsedDbBulkInsert == 1 ? "Enabled" : "Disabled");
                        }
                        else Log.Warning("Invalid value for 'data_bulk_insert' parameter. Using default value: {DefaultValue}", DEFAULT_VALUE_DbBulkInsert);
                        break;
                    case "debug":
                        break;
                    default:
                        Log.Error("Unknown parameter: {ParamName}", paramName);
                        break;
                }
            }
        }
    }
}
