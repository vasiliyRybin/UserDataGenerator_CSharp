using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserDataGenerator_C_
{
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
