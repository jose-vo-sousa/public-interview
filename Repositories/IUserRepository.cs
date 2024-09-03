namespace TinyBank2.Repositories
{
    using TinyBank2.Model;

    public interface IUserRepository
    {
        User GetUserById(Guid id);
        User GetUserByEmail(string email);
        bool UserExists(string email, string phonenumber);
        void AddUser(User user);
        void UpdateUser(User user);
        IEnumerable<User> GetAllUsers();
    }
}
