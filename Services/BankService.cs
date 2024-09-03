namespace TinyBank2.Services
{
    using TinyBank2.Mappers;
    using TinyBank2.Model;

    public class BankService : IBankService
    {
        // Memory data for application to work
        private readonly Dictionary<Guid, User> _users = new Dictionary<Guid, User>();
        private readonly List<Transaction> _transactions = new List<Transaction>();

        private readonly ILogger<BankService> logger;

        public BankService(ILogger<BankService> logger)
        {
            this.logger = logger;
            CreateDummyData();
        }

        public User? GetUserById(Guid id)
        {
            _users.TryGetValue(id, out var user);
            return user;
        }

        public User? GetUserByEmail(string email)
        {
            return _users.Values.FirstOrDefault(u => u.Email == email);
        }

        public bool DeactivateUser(Guid id)
        {
            //_users.Remove(id);

            _users[id].Active = false;
            return true;
        }

        public List<Transaction> GetTransactionHistory(Guid userId)
        {
            return _transactions.Where(t => t.OriginAccount == userId || t.DestinationAccount == userId).ToList();
        }

        public User AuthenticateUser(string email, string password)
        {
            var user = _users.Values.FirstOrDefault(u => u.Email == email && u.Password == password);
            return user;
        }

        public User CreateUser(UserCreationRequest userCreationRequest)
        {
            if (!_users.Values.Any(
                u => u.Email == userCreationRequest.Email || 
                u.PhoneNumber == userCreationRequest.PhoneNumber))
            {
                var user = userCreationRequest.ToUser();
                _users[user.Id] = user;

                return user;
            }

            var messagePrefix = $"{nameof(BankService)} > {nameof(CreateUser)} >";
            var accountAlreadyExistsMessage = $"{messagePrefix} Email '{userCreationRequest.Email}' or phone number '{userCreationRequest.PhoneNumber}' already in use.";
            this.LogWarning(accountAlreadyExistsMessage);

            // We don't want to expose PII in the exceptions, so it will leave with generic message
            throw new Exception("Something went wrong creating with the account creation process.");
        }

        public Transaction TransferMoney(Guid fromUserId, TransactionRequest transactionRequest)
        {
            var fromUser = GetAccountByUserId(fromUserId);
            var toUser = GetAccountByUserId(transactionRequest.DestinationAccount);

            ValidateAccountFunds(fromUser, transactionRequest.Amount);
            
            fromUser.AccountBalance -= transactionRequest.Amount;
            toUser.AccountBalance += transactionRequest.Amount;

            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                DateTime = DateTime.UtcNow,
                Amount = transactionRequest.Amount,
                Description = string.IsNullOrWhiteSpace(transactionRequest.Description) ? "Transfer" : transactionRequest.Description,
                OriginAccount = fromUserId,
                DestinationAccount = transactionRequest.DestinationAccount
            };

            _transactions.Add(transaction);
            fromUser.TransactionHistory.Add(transaction);
            toUser.TransactionHistory.Add(transaction);

            return transaction;
        }

        public Transaction DepositMoney(Guid id, DepositRequest depositRequest)
        {
            var userAccount = GetAccountByUserId(id);

            userAccount.AccountBalance += depositRequest.Amount;

            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                DateTime = DateTime.UtcNow,
                Amount = depositRequest.Amount,
                DestinationAccount = id,
                OriginAccount = Guid.Empty,
                Description = "Deposit"
            };

            _transactions.Add(transaction);
            userAccount.TransactionHistory.Add(transaction);

            return transaction;
        }

        public Transaction WithdrawMoney(Guid id, WithdrawRequest withdrawRequest)
        {
            var userAccount = GetAccountByUserId(id);

            ValidateAccountFunds(userAccount, withdrawRequest.Amount);

            userAccount.AccountBalance -= withdrawRequest.Amount;

            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                DateTime = DateTime.UtcNow,
                Amount = withdrawRequest.Amount,
                DestinationAccount = Guid.Empty,
                OriginAccount = id,
                Description = "Withdraw"
            };

            _transactions.Add(transaction);
            userAccount.TransactionHistory.Add(transaction);

            return transaction;
        }

        private void CreateDummyData()
        {
            var adminData = new User
            {
                AccountBalance = 999999,
                Active = true,
                Address = "Admin avenue 43",
                Email = "admin@email.com",
                Id = Guid.Parse("718b52e2-7051-4f3c-ab50-7d4104e20d0e"),
                Name = "admin",
                Password = "admin",
                PhoneNumber = "1234567890",
                TransactionHistory = new List<Transaction>()
            };

            _users[adminData.Id] = adminData;
        }

        private void LogWarning(string message)
        {
            logger.Log(LogLevel.Warning, message);
        }

        private User GetAccountByUserId(Guid id)
        {
            var messagePrefix = $"{nameof(BankService)} > {nameof(GetAccountByUserId)} >";
            var invalidUserIdMessage = $"{messagePrefix} Invalid account id '{id}'.";
            var accountDoesNotExistMessage = $"{messagePrefix} Account do not exist for the user with id '{id}'.";

            if (id == Guid.Empty)
            {
                this.LogWarning(invalidUserIdMessage);
                throw new Exception($"Invalid User Id.");
            }

            if (!_users.TryGetValue(id, out var user))
            {
                this.LogWarning(accountDoesNotExistMessage);
                throw new Exception("Account do not exist.");
            }

            return user;
        }

        private void ValidateAccountFunds(User userAccount, double amount)
        {
            if (userAccount.AccountBalance >= amount)
            {
                return;
            }

            var messagePrefix = $"{nameof(BankService)} > {nameof(ValidateAccountFunds)} >";
            var insufficientFundsMessage = $"{messagePrefix} Insufficient funds in the account with id '{userAccount.Id}'.";


            this.LogWarning(insufficientFundsMessage);
            throw new Exception("Insufficient funds.");
        }
    }
}
