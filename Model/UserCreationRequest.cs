namespace TinyBank2.Model
{
    using System.ComponentModel.DataAnnotations;

    public class UserCreationRequest
    {
        [Required]
        public required string Name {  get; set; }

        [Required]
        public required string Email { get; set; }

        [Required]
        public required string PhoneNumber { get; set; }

        [Required]
        public required string Password { get; set; }

        public string? Address { get; set; }
    }
}
