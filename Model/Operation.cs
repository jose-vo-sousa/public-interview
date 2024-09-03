using System.ComponentModel.DataAnnotations;

namespace TinyBank2.Model
{
    public class Operation
    {
        [Required]
        public required double Amount { get; set; }
    }
}
