using Moq;
using Uala.Challenge.Application.Tweets.Commands;
using Uala.Challenge.Domain.Entities;
using Uala.Challenge.Domain.Repositories;
using Uala.Challenge.Domain.Services;

namespace Uala.Challenge.UnitTests.Application.Tweets.Commands
{
    [TestFixture]
    public class CreateTweetCommandHandlerTests
    {
        private Mock<ITweetRepository> _tweetRepositoryMock;
        private Mock<IUserRepository> _userRepositoryMock;
        private Mock<ICacheService> _cacheServiceMock;
        private CreateTweetCommandHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _tweetRepositoryMock = new Mock<ITweetRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _cacheServiceMock = new Mock<ICacheService>();
            _handler = new CreateTweetCommandHandler(_tweetRepositoryMock.Object, _userRepositoryMock.Object, _cacheServiceMock.Object);
        }

        [Test]
        public void Handle_EmptyTweetContent_ShouldThrowArgumentException()
        {
            // Arrange
            var command = new CreateTweetCommand { UserId = Guid.NewGuid(), Content = string.Empty };

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.AreEqual("Tweet content cannot be empty", ex.Message);
        }

        [Test]
        public void Handle_NullTweetContent_ShouldThrowArgumentException()
        {
            // Arrange
            var command = new CreateTweetCommand { UserId = Guid.NewGuid(), Content = null };

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.AreEqual("Tweet content cannot be empty", ex.Message);
        }

        [Test]
        public void Handle_TweetContentTooLong_ShouldThrowArgumentException()
        {
            // Arrange
            var longContent = new string('a', 281);
            var command = new CreateTweetCommand { UserId = Guid.NewGuid(), Content = longContent };
            const int MaxTweetLength = 280;

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.AreEqual($"Tweet content cannot exceed {MaxTweetLength} characters", ex.Message);
        }

        [Test]
        public void Handle_UserNotFound_ShouldThrowArgumentException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new CreateTweetCommand { UserId = userId, Content = "Valid content" };

            _userRepositoryMock.Setup(r => r.Get(userId, It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync((User)null);

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.AreEqual("User not found", ex.Message);
            _userRepositoryMock.Verify(r => r.Get(userId, It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
            _tweetRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Tweet>()), Times.Never);
        }
    }
}
