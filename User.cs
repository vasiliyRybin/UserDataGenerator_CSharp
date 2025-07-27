using System.ComponentModel.DataAnnotations;

namespace UserDataGenerator_C_
{
    public class User
    {
        public int TaxID { get; set; } 
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string PassNumber { get; set; }
        public string Comment { get; set; }

        public User(int taxID, string firstName, string lastName, string email, string phoneNumber, string passNumber, string comment)
        {
            TaxID = taxID;
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            PhoneNumber = phoneNumber;
            PassNumber = passNumber;
            Comment = comment;
        }

        public override string ToString()
        {
            return $"{FirstName} {LastName}, Email: {Email}, Phone: {PhoneNumber}, Passport number: {PassNumber}, Comment: {Comment}";
        }

        public override bool Equals(object obj)
        {
            if (obj is User user)
            {
                return FirstName == user.FirstName &&
                       LastName == user.LastName &&
                       Email == user.Email &&
                       PhoneNumber == user.PhoneNumber &&
                       PassNumber == user.PassNumber &&
                       Comment == user.Comment;
            }
            else return false;
        }

        public override int GetHashCode()
        {
            int primeNumber = 23;
            return (
                        FirstName.GetHashCode() * primeNumber +
                        LastName.GetHashCode() * primeNumber +
                        Email.GetHashCode() * primeNumber +
                        PhoneNumber.GetHashCode() * primeNumber +
                        PassNumber.GetHashCode() * primeNumber +
                        Comment.GetHashCode() * primeNumber
                    );
        }
    }
}
