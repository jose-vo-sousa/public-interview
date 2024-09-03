namespace TinyBank2.Repositories
{
    using System.Collections.Concurrent;
    using TinyBank2.Model;

    public class UserRepository : IUserRepository
    {
        private readonly ConcurrentDictionary<Guid, User> _users = new ConcurrentDictionary<Guid, User>();

        public UserRepository()
        {
            var adminUser = new User
            {
                Active = true,
                Email = "admin@email.com",
                Name = "admin",
                Password = "admin",
                AccountBalance = 5000000,
                Address = "Admin st. 43",
                Id = Guid.Parse("718b52e2-7051-4f3c-ab50-7d4104e20d0e"),
                PhoneNumber = "000999111"
            };

            this.AddUser(adminUser);
        }

        public User GetUserById(Guid id) => _users.TryGetValue(id, out var user) ? user : null;

        public User GetUserByEmail(string email) => _users.Values.FirstOrDefault(u => u.Email == email);

        public bool UserExists(string email, string phonenumber) => _users.Values.FirstOrDefault(
            u => u.Email.ToLowerInvariant().Equals(email.ToLowerInvariant()) || u.PhoneNumber.Equals(phonenumber)) != null;

        public void AddUser(User user)
        {
            _users[user.Id] = user;
        }

        public void UpdateUser(User user)
        {
            _users[user.Id] = user;
        }

        public IEnumerable<User> GetAllUsers() => _users.Values;
    }
}
