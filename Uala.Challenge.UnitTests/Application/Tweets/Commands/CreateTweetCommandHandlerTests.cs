using Moq;
using Uala.Challenge.Application.Interfaces;
using Uala.Challenge.Application.Tweets.Commands;
using Uala.Challenge.Domain.Entities;
using Uala.Challenge.Domain.Events;
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
        private Mock<IKafkaProducer> _kafkaProducerMock;
        private CreateTweetCommandHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _tweetRepositoryMock = new Mock<ITweetRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _cacheServiceMock = new Mock<ICacheService>();
            _kafkaProducerMock = new Mock<IKafkaProducer>();
            _handler = new CreateTweetCommandHandler(
                _tweetRepositoryMock.Object, 
                _userRepositoryMock.Object, 
                _cacheServiceMock.Object,
                _kafkaProducerMock.Object);
        }

        [Test]
        public async Task Handle_ValidTweet_ShouldCreateTweetAndPublishEvent()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, Username = "testuser" };
            var command = new CreateTweetCommand { UserId = userId, Content = "Valid tweet content" };

            _userRepositoryMock.Setup(r => r.Get(userId, It.IsAny<bool>(), It.IsAny<bool>()))
                .ReturnsAsync(user);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(userId, result.UserId);
            Assert.AreEqual("Valid tweet content", result.Content);
            Assert.IsTrue(result.CreatedAt <= DateTime.UtcNow);

            _userRepositoryMock.Verify(r => r.Get(userId, It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
            _tweetRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Tweet>()), Times.Once);
            _kafkaProducerMock.Verify(k => k.PublishTweetCreatedAsync(It.IsAny<TweetCreatedEvent>()), Times.Once);
            _cacheServiceMock.Verify(c => c.RemovePatternAsync("timeline:*"), Times.Once);
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
            _kafkaProducerMock.Verify(k => k.PublishTweetCreatedAsync(It.IsAny<TweetCreatedEvent>()), Times.Never);
        }
    }
}
