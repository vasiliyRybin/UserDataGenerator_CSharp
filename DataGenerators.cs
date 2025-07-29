using System;
using System.Threading;
using System.Threading.Tasks;

namespace UserDataGenerator_C_
{
    internal class DataGenerators
    {
        private static readonly ThreadLocal<Random> threadRnd = new ThreadLocal<Random>(() => new Random());
        private static readonly string Letters = "AĄBCĆDEĘFGHIJKLŁMNŃOÓPRSŚTUWYZŹŻ";

        [LogMethod]
        public async Task<int> TaxesPayerNumberGenerator(int minValue, int maxValue, int InvalidTaxPayerRatio)
        {
            return await Task.Run(() =>
            {
                if (threadRnd.Value.Next(0, 100) > InvalidTaxPayerRatio)
                {
                    return threadRnd.Value.Next(minValue, maxValue);
                }
                else
                {
                    // Generate invalid tax payer number
                    return threadRnd.Value.Next((maxValue - 1) * -1, (minValue - 1) * -1);
                }
            });
        }

        [LogMethod]
        public async Task<string> KurwaPassNumberGenerator()
        {
            return await Task.Run
            (() =>
                {
                    string letter = Letters[threadRnd.Value.Next(Letters.Length - 1)].ToString();
                    return "ZZ" + letter + threadRnd.Value.Next(100000, 999999);
                }
            );
        }

        [LogMethod]
        public async Task<string> EmailGenerator(string firstName, string lastName)
        {
            return await Task.Run(() =>
            {
                string result = firstName.ToLower() + "." + lastName.ToLower() + ".";
                int amountOfPostfixLetters = threadRnd.Value.Next(1, 5);

                for (int i = 0; i < amountOfPostfixLetters; i++)
                {
                    result += Letters[threadRnd.Value.Next(Letters.Length - 1)];
                }

                return (result + "@test.com").ToLowerInvariant();
            });

        }

        [LogMethod]
        public async Task<int> PhoneNumberGenerator()
        {
            // Generate a random phone number
            return await Task.Run(() =>
            {
                return threadRnd.Value.Next(100000000, 999999999);
            });
        }
    }
}
