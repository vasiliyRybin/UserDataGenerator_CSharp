using RandomDataGenerator.FieldOptions;
using RandomDataGenerator.Randomizers;
using Serilog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;

namespace UserDataGenerator_C_
{
    public class Program
    {
        static void Main(string[] args)
        {            
            Dictionary<string, string> paths = new Dictionary<string, string>
            {
                { "PathToDB", "TestUserData.db" },
                { "PathToCSV", "TestUserData.csv" },
                { "PathToLog", "Log.txt" }
            };

            const int START_INDEX = 0;
            int totalAmount = 0;
            string executionPath = AppDomain.CurrentDomain.BaseDirectory;
            string logPath = executionPath + "Logs\\" + paths["PathToLog"];
            string generatedDataPath = executionPath + "Data\\";
            string DBPath = generatedDataPath + paths["PathToDB"];
            string userRecordString = string.Empty;

            Stopwatch sw = new Stopwatch();
            sw.Start();
            
            LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logPath, rollingInterval: RollingInterval.Day, outputTemplate: "[{Timestamp:dd-MM-yyyy HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.Console();
            Log.Logger = loggerConfiguration.CreateLogger();

            try
            {
                Directory.CreateDirectory(logPath);
                Directory.CreateDirectory(generatedDataPath);
                Log.Information("");
                Log.Information("");
                Log.Information("*****************************************************");
                Log.Information("Application was started at {Time}", CurrentDateTime_Formatted());

                var parameters = new StartupParameters(args);

                HashSet<int> taxesPayerNumberSet = new HashSet<int>();
                HashSet<string> passNumberSet = new HashSet<string>();
                HashSet<User> usersSet = new HashSet<User>();

                HashSet<User> usersInDBSet = new HashSet<User>();

                // Create database and table if it does not exist 
                DataWorker.CreateDatabase_IfNotExists(DBPath);
                bool isUsersTableExists = DataWorker.CreateUsersTable_IfNotExists(DBPath);

                if (isUsersTableExists && parameters.InMemoryProcessing)
                {
                    usersInDBSet = new HashSet<User>(DataWorker.GetUsersFromDB(DBPath));
                }

                int i = START_INDEX;
                var dataGenerators = new DataGenerators();
                var rnd = new Random().Next(StartupParameters.MIN_VALID_TAXES_PAYER_NUMBER, StartupParameters.MAX_VALID_TAXES_PAYER_NUMBER);

                while (i < parameters.Amount)
                {
                    bool isTaxIdAlreadyExists = false;
                    int taxId = dataGenerators.TaxesPayerNumberGenerator(StartupParameters.MIN_VALID_TAXES_PAYER_NUMBER, 
                                                                         StartupParameters.MAX_VALID_TAXES_PAYER_NUMBER,
                                                                         parameters.InvalidTaxPayerRatio);

                    if (parameters.InMemoryProcessing) isTaxIdAlreadyExists = usersInDBSet.Select(x => x.TaxID).Contains(taxId);
                    else isTaxIdAlreadyExists = DataWorker.GetDataCountFromTable(DBPath, "TaxID", "Users", taxId.ToString()) > 0;

                    if(!isTaxIdAlreadyExists)
                    {
                        taxesPayerNumberSet.Add(taxId);
                        i = taxesPayerNumberSet.Count;
                    }
                }
                Log.Information("Amount of unique Tax IDs generated: {Count}", taxesPayerNumberSet.Count);
                
                i = START_INDEX;
                while (i < parameters.Amount)
                {
                    bool isPassNumberAlreadyExists = false;
                    string passNumber = dataGenerators.KurwaPassNumberGenerator();

                    if (parameters.InMemoryProcessing) isPassNumberAlreadyExists = usersInDBSet.Select(x => x.PassNumber).Contains(passNumber);
                    else isPassNumberAlreadyExists = DataWorker.GetDataCountFromTable(DBPath, "TaxID", "Users", passNumber) > 0;

                    if (!isPassNumberAlreadyExists)
                    {
                        passNumberSet.Add(passNumber);
                        i = passNumberSet.Count;
                    }
                }
                Log.Information("Amount of unique passport numbers generated: {Count}", passNumberSet.Count);

                i = START_INDEX;
                var randomizerFirstName = RandomizerFactory.GetRandomizer(new FieldOptionsFirstName());
                var randomizerLastName = RandomizerFactory.GetRandomizer(new FieldOptionsLastName());

                while (i < parameters.Amount)
                {
                    bool isEmailExists = false;
                    string firstName = randomizerFirstName.Generate();
                    string lastName = randomizerLastName.Generate();
                    int taxID = taxesPayerNumberSet.ElementAt(i);
                    string passNumber = passNumberSet.ElementAt(i);

                    User user = new User
                    (
                        taxID,
                        firstName,
                        lastName,
                        firstName.ToLower() + "." + lastName.ToLower() + "@test.com",
                        "+" + dataGenerators.PhoneNumerGenerator(),
                        passNumber,
                        string.Empty
                    );

                    do
                    {
                        if (parameters.InMemoryProcessing) isEmailExists = usersInDBSet.Select(x => x.Email).Contains(user.Email);
                        else isEmailExists = DataWorker.GetDataCountFromTable(DBPath, "Email", "Users", user.Email) > 0;
                        
                        if(isEmailExists) user.Email = dataGenerators.EmailGenerator(firstName, lastName);
                        userRecordString = user.ToString();

                    } while (isEmailExists);

                    if (taxID < StartupParameters.MIN_VALID_TAXES_PAYER_NUMBER) user.Comment = "O kurwa! Popierdolony numer podatnika";

                    usersSet.Add(user);

                    if (!parameters.DbBulkInsert)
                    {
                        switch (parameters.OutputTo)
                        {
                            case 0: // Write to CSV
                                DataWorker.WriteDataToCSV(generatedDataPath + paths["PathToCSV"], usersSet);
                                break;
                            case 1: // Write to DB
                                DataWorker.InsertUserToDB(DBPath, usersSet);
                                break;
                            case 2: // Both options (To CSV and DB)
                                DataWorker.WriteDataToCSV(generatedDataPath + paths["PathToCSV"], usersSet);
                                DataWorker.InsertUserToDB(DBPath, usersSet);
                                break;
                            default:
                                break;
                        }
                        usersSet.Clear();
                        i++;
                        totalAmount += 1;
                    }
                    else i = usersSet.Count;
                }
                Log.Information("Amount of unique users generated: {Count}", totalAmount);
            }
			catch (Exception ex)
			{
                Console.WriteLine(ex.Message);
                Log.Fatal("App crashed!");
                Log.Fatal("Cause: " + ex.Message);
                Log.Debug(userRecordString);
            }
            finally
            {
                sw.Stop();
            }
        }

        private static string CurrentDateTime_Formatted()
        {
            return DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString();
        }
    }
}
