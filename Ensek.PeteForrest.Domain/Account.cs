namespace Ensek.PeteForrest.Domain {
    public class Account(string? firstName, string? lastName) {
        public int AccountId { get; set; }

        public string? FirstName { get; set; } = firstName;

        public string? LastName { get; set; } = lastName;
    }
}
