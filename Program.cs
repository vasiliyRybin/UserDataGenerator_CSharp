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
        static async Task Main(string[] args)
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
                HashSet<string> passNumberSetDB = new HashSet<string>();
                HashSet<int> taxIdDBSet = new HashSet<int>();
                HashSet<string> emailSetDB = new HashSet<string>();

                // Create database and table if it does not exist 
                DataWorker.CreateDatabase_IfNotExists(DBPath);
                bool isUsersTableExists = await DataWorker.CreateUsersTable_IfNotExistsAsync(DBPath);

                if (isUsersTableExists && parameters.InMemoryProcessing)
                {
                    usersInDBSet = new HashSet<User>(await DataWorker.GetUsersFromDBAsync(DBPath));
                    taxIdDBSet = new HashSet<int>(usersInDBSet.Select(x => x.TaxID));
                    passNumberSetDB = new HashSet<string>(usersInDBSet.Select(x => x.PassNumber));
                    emailSetDB = new HashSet<string>(usersInDBSet.Select(x => x.Email));
                }

                int i = START_INDEX;
                var dataGenerators = new DataGenerators();
                var rnd = new Random().Next(StartupParameters.MIN_VALID_TAXES_PAYER_NUMBER, StartupParameters.MAX_VALID_TAXES_PAYER_NUMBER);

                while (i < parameters.Amount)
                {
                    bool isTaxIdAlreadyExists = false;
                    int taxId = await dataGenerators.TaxesPayerNumberGenerator(StartupParameters.MIN_VALID_TAXES_PAYER_NUMBER, 
                                                                         StartupParameters.MAX_VALID_TAXES_PAYER_NUMBER,
                                                                         parameters.InvalidTaxPayerRatio);

                    if (parameters.InMemoryProcessing) isTaxIdAlreadyExists = taxIdDBSet.Contains(taxId) || taxesPayerNumberSet.Contains(taxId);
                    else isTaxIdAlreadyExists = await DataWorker.GetDataCountFromTable(DBPath, "TaxID", "Users", taxId.ToString()) > 0 || taxesPayerNumberSet.Contains(taxId);

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
                    string passNumber = await dataGenerators.KurwaPassNumberGenerator();

                    if (parameters.InMemoryProcessing) isPassNumberAlreadyExists = passNumberSetDB.Contains(passNumber);
                    else isPassNumberAlreadyExists = await DataWorker.GetDataCountFromTable(DBPath, "TaxID", "Users", passNumber) > 0;

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

                    string email = string.Empty;                    
                    if(lastName.Contains("\'")) 
                        email = firstName.ToLower() + "." + lastName.Replace("\'", "").ToLower() + "@test.com";
                    else email = firstName.ToLower() + "." + lastName.ToLower() + "@test.com";


                    emailSetDB.Add(email);

                    User user = new User
                    (
                        taxID,
                        firstName,
                        lastName,
                        email,
                        "+" + await dataGenerators.PhoneNumberGenerator(),
                        passNumber,
                        string.Empty
                    );

                    do
                    {
                        if (parameters.InMemoryProcessing) isEmailExists = emailSetDB.Contains(user.Email) || emailSetDB.Contains(user.Email);
                        else isEmailExists = await DataWorker.GetDataCountFromTable(DBPath, "Email", "Users", user.Email) > 0 || emailSetDB.Contains(user.Email);
                        
                        if(isEmailExists) user.Email = lastName.Contains("\'") ? await dataGenerators.EmailGenerator(firstName, lastName.Replace("\'", "")) 
                                                                               : await dataGenerators.EmailGenerator(firstName, lastName);
                        userRecordString = user.ToString();

                    } while (isEmailExists);

                    if (taxID < StartupParameters.MIN_VALID_TAXES_PAYER_NUMBER) user.Comment = "O kurwa! Popierdolony numer podatnika";

                    usersSet.Add(user);

                    if (!parameters.DbBulkInsert)
                    {
                        try
                        {
                            await DataWorker.WriteDataAsync(parameters, generatedDataPath, paths, usersSet);
                            usersSet.Clear();
                            i++;
                            totalAmount += 1;

                            /*
                             * Idea is: each 1000 records recreate index for better performance
                             */
                            if (totalAmount % 1000 == 1)
                            {
                                await DataWorker.DropIndexIfExists(DBPath, "IX_Users_TaxID");
                                await DataWorker.DropIndexIfExists(DBPath, "IX_Users_Email");
                                await DataWorker.DropIndexIfExists(DBPath, "IX_Users_PassNumber");

                                await DataWorker.CreateIndexIfNotExists(DBPath, "IX_Users_TaxID", "Users", "TaxID");
                                await DataWorker.CreateIndexIfNotExists(DBPath, "IX_Users_Email", "Users", "Email");
                                await DataWorker.CreateIndexIfNotExists(DBPath, "IX_Users_PassNumber", "Users", "PassNumber");
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Error inserting user: {Message}", ex.Message);
                            Log.Error(userRecordString);
                        }
                    }
                    else
                    {
                        i = usersSet.Count;
                        totalAmount += 1;
                    }
                    CalculateTaskCompletion(parameters.Amount, i);
                }

                try
                {
                    if (parameters.DbBulkInsert)
                    {
                        Log.Information("Bulk insert to DB is enabled. Inserting all records at once...");
                        await DataWorker.WriteDataAsync(parameters, generatedDataPath, paths, usersSet);
                    }

                    Log.Information("Amount of unique users generated: {Count}", totalAmount);

                    await DataWorker.VacuumDatabase(DBPath);
                }
                catch (Exception ex)
                {
                    Log.Error("Error during bulk insert: {Message}", ex.Message);
                    Log.Error(userRecordString);
                }
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

        private static void CalculateTaskCompletion(int amount, int i)
        {
            int divisionRemainder = amount / 20;
            if (divisionRemainder > 0 && i % divisionRemainder == 0)
            {
                int percentComplegted = i * 100 / amount;
                Log.Information("Task completed: {Percent}%", percentComplegted);
            }
        }
    }
}
