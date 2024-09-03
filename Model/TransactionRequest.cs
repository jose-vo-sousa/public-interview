namespace TinyBank2.Model
{
    using System.ComponentModel.DataAnnotations;

    public class TransactionRequest : Operation
    {
        public string? Description { get; set; }

        [Required] 
        public required Guid DestinationAccount { get; set; }
    }
}
