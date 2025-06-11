using Moq;
using Uala.Challenge.Application.Tweets.Queries;
using Uala.Challenge.Domain.Entities;
using Uala.Challenge.Domain.Repositories;
using Uala.Challenge.Domain.Services;

namespace Uala.Challenge.UnitTests.Application.Tweets.Queries
{
    [TestFixture]
    public class GetTimelineQueryHandlerTests
    {
        private Mock<ITweetRepository> _tweetRepositoryMock;
        private Mock<IUserRepository> _userRepositoryMock;
        private Mock<ICacheService> _caceServiceMock;

        private GetTimelineQueryHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _tweetRepositoryMock = new Mock<ITweetRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _caceServiceMock = new Mock<ICacheService>();
            _handler = new GetTimelineQueryHandler(_tweetRepositoryMock.Object, _userRepositoryMock.Object, _caceServiceMock.Object);
        }

        [Test]
        public async Task Handle_UserExistsAndHasTweets_ShouldReturnPagedResultOfTimelineQueryResponse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var pageNumber = 1;
            var pageSize = 10;
            var query = new GetTimelineQuery(userId, pageNumber, pageSize);

            var followedUser1Id = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                Following = new List<User> { new User { Id = followedUser1Id, Username = "followed1" } }
            };
            _userRepositoryMock.Setup(r => r.Get(userId, It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(user);

            var tweetsFromRepo = new List<Tweet>
            {
                new Tweet { Id = Guid.NewGuid(), Content = "Tweet 1 by user", UserId = userId, CreatedAt = DateTime.UtcNow.AddHours(-1) },
                new Tweet { Id = Guid.NewGuid(), Content = "Tweet 2 by followed", UserId = followedUser1Id, CreatedAt = DateTime.UtcNow.AddHours(-2) }
            };
            var totalTweets = tweetsFromRepo.Count;

            _tweetRepositoryMock.Setup(r => r.GetTimelineAsync(It.Is<IEnumerable<Guid>>(ids => ids.Contains(userId) && ids.Contains(followedUser1Id)), pageNumber, pageSize))
                .ReturnsAsync(new Tuple<IEnumerable<Tweet>, int>(tweetsFromRepo, totalTweets));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(pageNumber, result.PageNumber);
            Assert.AreEqual(pageSize, result.PageSize);
            Assert.AreEqual(totalTweets, result.TotalItems);
            Assert.AreEqual((int)Math.Ceiling((double)totalTweets / pageSize), result.TotalPages);
            Assert.AreEqual(tweetsFromRepo.Count, result.Items.Count);
            Assert.IsTrue(result.Items.Any(t => t.Content == "Tweet 1 by user"));
            Assert.IsTrue(result.Items.Any(t => t.Content == "Tweet 2 by followed"));

            _userRepositoryMock.Verify(r => r.Get(userId, It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
            _tweetRepositoryMock.Verify(r => r.GetTimelineAsync(It.IsAny<IEnumerable<Guid>>(), pageNumber, pageSize), Times.Once);
        }

        [Test]
        public async Task Handle_UserExistsButNoTweetsFound_ShouldReturnEmptyPagedResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var pageNumber = 1;
            var pageSize = 10;
            var query = new GetTimelineQuery(userId, pageNumber, pageSize);

            var user = new User { Id = userId, Username = "testuser", Following = new List<User>() }; // No followings
            _userRepositoryMock.Setup(r => r.Get(userId, It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(user);

            var emptyTweetsList = new List<Tweet>();
            var totalTweets = 0;

            _tweetRepositoryMock.Setup(r => r.GetTimelineAsync(It.Is<IEnumerable<Guid>>(ids => ids.Contains(userId) && ids.Count() == 1), pageNumber, pageSize))
                .Returns(Task.FromResult(new Tuple<IEnumerable<Tweet>, int>(emptyTweetsList.AsEnumerable(), totalTweets)));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(pageNumber, result.PageNumber);
            Assert.AreEqual(pageSize, result.PageSize);
            Assert.AreEqual(totalTweets, result.TotalItems);
            Assert.AreEqual(0, result.TotalPages);
            Assert.IsEmpty(result.Items);

            _userRepositoryMock.Verify(r => r.Get(userId, It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
            _tweetRepositoryMock.Verify(r => r.GetTimelineAsync(It.IsAny<IEnumerable<Guid>>(), pageNumber, pageSize), Times.Once);
        }

        [Test]
        public void Handle_UserNotFound_ShouldThrowArgumentNullException()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var query = new GetTimelineQuery(userId, 1, 10);

            _userRepositoryMock.Setup(r => r.Get(userId, It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync((User)null);

            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(() => _handler.Handle(query, CancellationToken.None));

            _userRepositoryMock.Verify(r => r.Get(userId, It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
            _tweetRepositoryMock.Verify(r => r.GetTimelineAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Test]
        public async Task Handle_UserHasNoFollowings_ShouldFetchOnlyOwnTweetsAndReturnPagedResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var pageNumber = 1;
            var pageSize = 10;
            var query = new GetTimelineQuery(userId, pageNumber, pageSize);

            var user = new User { Id = userId, Username = "lonelyuser", Following = new List<User>() }; // No one followed
            _userRepositoryMock.Setup(r => r.Get(userId, It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(user);

            var ownTweets = new List<Tweet>
            {
                new Tweet { Id = Guid.NewGuid(), Content = "My own tweet", UserId = userId, CreatedAt = DateTime.UtcNow }
            };
            var totalTweets = ownTweets.Count;

            _tweetRepositoryMock.Setup(r => r.GetTimelineAsync(It.Is<IEnumerable<Guid>>(ids => ids.Count() == 1 && ids.First() == userId), pageNumber, pageSize))
                .Returns(Task.FromResult( new Tuple<IEnumerable<Tweet>, int>(ownTweets.AsEnumerable(), totalTweets)));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(totalTweets, result.Items.Count);
            Assert.AreEqual(userId, result.Items.First().UserId);
            Assert.AreEqual("My own tweet", result.Items.First().Content);

            _userRepositoryMock.Verify(r => r.Get(userId, It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
            _tweetRepositoryMock.Verify(r => r.GetTimelineAsync(It.Is<IEnumerable<Guid>>(ids => ids.Count() == 1 && ids.First() == userId), pageNumber, pageSize), Times.Once);
        }

        [Test]
        public async Task Handle_UserHasNullFollowings_ShouldFetchOnlyOwnTweetsAndReturnPagedResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var pageNumber = 1;
            var pageSize = 10;
            var query = new GetTimelineQuery(userId, pageNumber, pageSize);

            var user = new User { Id = userId, Username = "userwithnullfollowing", Following = null }; // Following collection is null
            _userRepositoryMock.Setup(r => r.Get(userId, It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(user);

            var ownTweets = new List<Tweet>
            {
                new Tweet { Id = Guid.NewGuid(), Content = "My only tweet", UserId = userId, CreatedAt = DateTime.UtcNow }
            };
            var totalTweets = ownTweets.Count;

            _tweetRepositoryMock.Setup(r => r.GetTimelineAsync(It.Is<IEnumerable<Guid>>(ids => ids.Count() == 1 && ids.First() == userId), pageNumber, pageSize))
                .ReturnsAsync(new Tuple<IEnumerable<Tweet>, int>(ownTweets, totalTweets));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(totalTweets, result.Items.Count);
            Assert.AreEqual(userId, result.Items.First().UserId);
            Assert.AreEqual("My only tweet", result.Items.First().Content);

            _userRepositoryMock.Verify(r => r.Get(userId, It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
            _tweetRepositoryMock.Verify(r => r.GetTimelineAsync(It.Is<IEnumerable<Guid>>(ids => ids.Count() == 1 && ids.First() == userId), pageNumber, pageSize), Times.Once);
        }
    }
}
