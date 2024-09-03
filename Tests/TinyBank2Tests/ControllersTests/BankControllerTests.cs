using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Security.Claims;
using TinyBank2.Controllers;
using TinyBank2.Model;
using TinyBank2.Services;

namespace TinyBank2.Tests.ControllersTests
{
    [TestClass]
    public class BankControllerTests
    {
        private readonly Mock<IBankService> _bankServiceMock;
        private readonly Mock<ILogger<BankController>> _loggerServiceMock;
        private readonly BankController _controller;
        private readonly User? mockedUserData = null;

        public BankControllerTests()
        {
            _bankServiceMock = new Mock<IBankService>();
            _loggerServiceMock = new Mock<ILogger<BankController>>();
            _controller = new BankController(_bankServiceMock.Object, _loggerServiceMock.Object);

            mockedUserData = MockUserData();
        }

        [TestMethod]
        public void GetBalance_WhenNotAuthorized_ShouldReturnUnauthorizedResult()
        {
            // Arrange

            // Act
            var act = _controller.GetBalanceAsync(mockedUserData.Id.ToString());

            // Assert
            Assert.IsNotNull(act);
            Assert.IsInstanceOfType(act.Result, typeof(UnauthorizedResult));
            var result = act.Result as UnauthorizedResult;
            Assert.AreEqual(401, result.StatusCode);
        }

        [TestMethod]
        public void GetBalance_WhenAuthorized_ShouldReturnOkWithAccountBalanceValue()
        {
            // Arrange
            var claimsPrincipal = new ClaimsPrincipal(
                new ClaimsIdentity(
                    new Claim[]
                    {
                        new Claim(ClaimTypes.Name, mockedUserData.Id.ToString()),
                    }, "mock"));

            var httpContextMock = new Mock<HttpContext>();
            httpContextMock
                .Setup(ctx => ctx.User)
                .Returns(claimsPrincipal);

            var controllerContext = new ControllerContext()
            {
                HttpContext = httpContextMock.Object
            };

            var controller = new BankController(_bankServiceMock.Object, _loggerServiceMock.Object)
            {
                ControllerContext = controllerContext
            };

            _bankServiceMock
                .Setup(p => p.GetUserById(mockedUserData.Id))
                .Returns(mockedUserData)
                .Verifiable();

            // Act
            var act = controller.GetBalanceAsync(mockedUserData.Id.ToString());

            // Assert
            Assert.IsNotNull(act);
            Assert.IsInstanceOfType(act.Result, typeof(OkObjectResult));
            var result = act.Result as OkObjectResult;
            Assert.AreEqual(mockedUserData.AccountBalance, result.Value);

            _bankServiceMock.VerifyAll();
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
