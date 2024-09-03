namespace TinyBank2.Model
{
    public class User
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Email { get; set; }
        public string? Address { get; set; }
        public string? PhoneNumber { get; set; }
        public required string Password { get; set; }
        public double AccountBalance { get; set; }
        public required bool Active { get; set; }
        public List<Transaction> TransactionHistory { get; set; } = new List<Transaction>();
    }
}
