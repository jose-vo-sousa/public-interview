using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TinyBank2.Model;
using TinyBank2.Repositories;
using TinyBank2.Services;

namespace TinyBank2Tests.ServicesTests
{
    [TestClass]
    public class BankServiceTests
    {
        private readonly Mock<IUserRepository> userRepositoryMock;
        private readonly Mock<ITransactionRepository> transactionRepositoryMock;
        private readonly Mock<ILogger<BankService>> loggerServiceMock;
        private readonly User? mockedUserData = null;
        private readonly BankService _service;

        public BankServiceTests()
        {
            userRepositoryMock = new Mock<IUserRepository>();
            transactionRepositoryMock = new Mock<ITransactionRepository>();
            loggerServiceMock = new Mock<ILogger<BankService>>();
            _service = new BankService(loggerServiceMock.Object, userRepositoryMock.Object, transactionRepositoryMock.Object);

            mockedUserData = MockUserData();
        }

        [TestMethod]
        public void DepositMoney_DefaultBehavior_ShouldReturnTransaction()
        {
            // Arrange
            var mockedDepositRequest = new DepositRequest { Amount = 100 };

            userRepositoryMock
                .Setup(p => p.GetUserById(mockedUserData.Id))
                .Returns(mockedUserData)
                .Verifiable();

            userRepositoryMock
                .Setup(p => p.UpdateUser(It.IsAny<User>()))
                .Verifiable(Times.Once);

            transactionRepositoryMock
                .Setup(p => p.AddTransaction(It.IsAny<Transaction>()))
                .Verifiable(Times.Once);

            // Act
            var act = _service.DepositMoney(mockedUserData.Id, mockedDepositRequest);

            // Assert
            Assert.IsNotNull(act);
            Assert.IsInstanceOfType(act, typeof(Transaction));
            Assert.AreEqual(mockedDepositRequest.Amount, act.Amount);
            Assert.AreEqual(mockedUserData.Id, act.DestinationAccount);

            userRepositoryMock.VerifyAll();
            transactionRepositoryMock.VerifyAll();
        }

        private User MockUserData()
        {
            return new User
            {
                Active = true,
                AccountBalance = 2000,
                Email = "user@email.com",
                Name = "Test",
                Password = "Password",
                PhoneNumber = "1234567890",
                Id = Guid.NewGuid()
            };
        }
    }
}
