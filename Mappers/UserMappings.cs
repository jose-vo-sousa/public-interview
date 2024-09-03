using TinyBank2.Model;

namespace TinyBank2.Mappers
{
    public static class UserMappings
    {
        internal static User ToUser(this UserCreationRequest userCreationRequest)
        {
            return new User
            {
                Id = Guid.NewGuid(),
                Active = true,
                AccountBalance = 0,
                Email = userCreationRequest.Email,
                Address = userCreationRequest.Address,
                Name = userCreationRequest.Name,
                PhoneNumber = userCreationRequest.PhoneNumber,
                Password = userCreationRequest.Password,
                TransactionHistory = new List<Transaction>(),
            };
        }
    }
}
