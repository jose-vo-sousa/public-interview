namespace TinyBank2.Model
{
    using System.ComponentModel.DataAnnotations;

    public class UserLoginRequest
    {
        [Required]
        public required string Email { get; set; }

        [Required]
        public required string Password { get; set; }
    }
}
