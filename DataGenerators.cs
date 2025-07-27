using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserDataGenerator_C_
{
    internal class DataGenerators
    {
        private static readonly Random rnd = new Random();
        private static readonly string Letters = "AĄBCĆDEĘFGHIJKLŁMNŃOÓPRSŚTUWYZŹŻ";
        public DataGenerators()
        {
            
        }

        public int TaxesPayerNumberGenerator(int minValue, int maxValue, int InvalidTaxPayerRatio)
        {
            if (rnd.Next(0, 100) > InvalidTaxPayerRatio)
            {
                return rnd.Next(minValue, maxValue);
            }
            else
            {
                // Generate invalid tax payer number
                return rnd.Next((maxValue - 1) * -1, (minValue - 1) * -1);
            }
        }

        public string KurwaPassNumberGenerator()
        {
            string letter = Letters[rnd.Next(Letters.Length - 1)].ToString();
            return "ZZ" + letter + rnd.Next(100000, 999999);
        }
        public string EmailGenerator(string firstName, string lastName)
        {
            string result = firstName.ToLower() + "." + lastName.ToLower();
            int amountOfPostfixLetters = rnd.Next(1, 5);

            for (int i = 0; i < amountOfPostfixLetters; i++)
            {
                result += Letters[rnd.Next(Letters.Length - 1)];
            }

            return result + "@test.com";
        }

        public int PhoneNumerGenerator()
        {
            // Generate a random phone number
            return rnd.Next(100000000, 999999999);
        }
    }
}
