using Moq;
using Uala.Challenge.Application.Users.Queries;
using Uala.Challenge.Domain.Entities;
using Uala.Challenge.Domain.Repositories;

namespace Uala.Challenge.UnitTests.Application.Users.Queries
{
    [TestFixture]
    public class GetAllUsersQueryHandlerTests
    {
        private Mock<IUserRepository> _userRepositoryMock;
        private GetAllUsersQueryHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _handler = new GetAllUsersQueryHandler(_userRepositoryMock.Object);
        }

        [Test]
        public async Task Handle_WhenUsersExist_ShouldReturnMappedUserCollection()
        {
            // Arrange
            var users = new List<User>
            {
                new User { Id = Guid.NewGuid(), Username = "user1" },
                new User { Id = Guid.NewGuid(), Username = "user2" }
            };
            _userRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(users);
            var query = new GetAllUsersQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
            var firstUser = result.First();
            var originalFirstUser = users.First();
            Assert.AreEqual(originalFirstUser.Id, firstUser.Id);
            Assert.AreEqual(originalFirstUser.Username, firstUser.Username);
        }

        [Test]
        public async Task Handle_WhenNoUsersExist_ShouldReturnEmptyCollection()
        {
            // Arrange
            var emptyUserList = new List<User>();
            _userRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(emptyUserList);
            var query = new GetAllUsersQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsEmpty(result);
        }
    }
}
