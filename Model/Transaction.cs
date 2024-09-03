namespace TinyBank2.Model
{
    public class Transaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime DateTime { get; set; } = DateTime.Now;
        public double Amount { get; set; }
        public string? Description { get; set; }
        public Guid OriginAccount { get; set; }
        public Guid DestinationAccount { get; set; }
    }
}
