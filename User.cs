namespace UserDataGenerator_C_
{
    public class User
    {
        public int TaxID { get; }
        public string FirstName { get; }
        public string LastName { get; }
        public string Email { get; set; }
        public string PhoneNumber { get; }
        public string PassNumber { get; }
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
            return $"UserID: {TaxID}, {FirstName} {LastName}, Email: {Email}, Phone: {PhoneNumber}, Passport number: {PassNumber}, Comment: {Comment}";
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
