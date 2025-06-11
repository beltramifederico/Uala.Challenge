using Moq;
using Uala.Challenge.Application.Interfaces;
using Uala.Challenge.Application.Tweets.Queries;
using Uala.Challenge.Domain.Common;
using Uala.Challenge.Domain.Entities;
using Uala.Challenge.Domain.Repositories;
using Uala.Challenge.Domain.Services;

namespace Uala.Challenge.UnitTests.Application.Tweets.Queries
{
    [TestFixture]
    public class GetTimelineQueryHandlerTests
    {
        private Mock<ITimelineRepository> _timelineRepositoryMock;
        private Mock<IUserRepository> _userRepositoryMock;
        private Mock<ICacheService> _cacheServiceMock;

        private GetTimelineQueryHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _timelineRepositoryMock = new Mock<ITimelineRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _cacheServiceMock = new Mock<ICacheService>();
            _handler = new GetTimelineQueryHandler(_timelineRepositoryMock.Object, _userRepositoryMock.Object, _cacheServiceMock.Object);
        }

        [Test]
        public async Task Handle_UserExistsAndHasTimeline_ShouldReturnPagedResultOfTimelineQueryResponse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var authorId1 = Guid.NewGuid();
            var authorId2 = Guid.NewGuid();
            var pageNumber = 1;
            var pageSize = 10;
            var query = new GetTimelineQuery(userId, pageNumber, pageSize);

            var timelinesFromRepo = new List<Timeline>
            {
                new Timeline 
                { 
                    Id = Guid.NewGuid(), 
                    UserId = userId, 
                    TweetId = Guid.NewGuid(),
                    AuthorId = authorId1,
                    Content = "Tweet 1 by author1", 
                    CreatedAt = DateTime.UtcNow.AddHours(-1) 
                },
                new Timeline 
                { 
                    Id = Guid.NewGuid(), 
                    UserId = userId, 
                    TweetId = Guid.NewGuid(),
                    AuthorId = authorId2,
                    Content = "Tweet 2 by author2", 
                    CreatedAt = DateTime.UtcNow.AddHours(-2) 
                }
            };
            var totalTimelines = timelinesFromRepo.Count;

            _timelineRepositoryMock.Setup(r => r.GetTimelineAsync(userId, pageNumber, pageSize))
                .ReturnsAsync((timelinesFromRepo, (long)totalTimelines));

            // Setup user repository to return usernames
            var users = new List<User>
            {
                new User { Id = authorId1, Username = "author1" },
                new User { Id = authorId2, Username = "author2" }
            };
            _userRepositoryMock.Setup(r => r.GetUsersByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(users);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(pageNumber, result.PageNumber);
            Assert.AreEqual(pageSize, result.PageSize);
            Assert.AreEqual(totalTimelines, result.TotalItems);
            Assert.AreEqual((int)Math.Ceiling((double)totalTimelines / pageSize), result.TotalPages);
            Assert.AreEqual(timelinesFromRepo.Count, result.Items.Count);
            Assert.IsTrue(result.Items.Any(t => t.Content == "Tweet 1 by author1"));
            Assert.IsTrue(result.Items.Any(t => t.Content == "Tweet 2 by author2"));
            Assert.IsTrue(result.Items.Any(t => t.Username == "author1"));
            Assert.IsTrue(result.Items.Any(t => t.Username == "author2"));

            _timelineRepositoryMock.Verify(r => r.GetTimelineAsync(userId, pageNumber, pageSize), Times.Once);
            _userRepositoryMock.Verify(r => r.GetUsersByIdsAsync(It.IsAny<IEnumerable<Guid>>()), Times.Once);
        }

        [Test]
        public async Task Handle_UserExistsButNoTimelineFound_ShouldReturnEmptyPagedResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var pageNumber = 1;
            var pageSize = 10;
            var query = new GetTimelineQuery(userId, pageNumber, pageSize);

            var emptyTimelinesList = new List<Timeline>();
            var totalTimelines = 0;

            _timelineRepositoryMock.Setup(r => r.GetTimelineAsync(userId, pageNumber, pageSize))
                .ReturnsAsync((emptyTimelinesList, (long)totalTimelines));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(pageNumber, result.PageNumber);
            Assert.AreEqual(pageSize, result.PageSize);
            Assert.AreEqual(totalTimelines, result.TotalItems);
            Assert.AreEqual(0, result.TotalPages);
            Assert.AreEqual(0, result.Items.Count);

            _timelineRepositoryMock.Verify(r => r.GetTimelineAsync(userId, pageNumber, pageSize), Times.Once);
            _userRepositoryMock.Verify(r => r.GetUsersByIdsAsync(It.IsAny<IEnumerable<Guid>>()), Times.Never);
        }

        [Test]
        public async Task Handle_WithCacheHit_ShouldReturnCachedResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var pageNumber = 1;
            var pageSize = 10;
            var query = new GetTimelineQuery(userId, pageNumber, pageSize);
            var cacheKey = $"timeline:{userId}:page:{pageNumber}:size:{pageSize}";

            var cachedResult = new PagedResult<GetTimelineQueryResponse>
            {
                Items = new List<GetTimelineQueryResponse>
                {
                    new GetTimelineQueryResponse 
                    { 
                        Id = Guid.NewGuid(), 
                        Content = "Cached tweet", 
                        UserId = Guid.NewGuid(),
                        Username = "cacheduser",
                        CreatedAt = DateTime.UtcNow 
                    }
                },
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalItems = 1,
                TotalPages = 1
            };

            _cacheServiceMock.Setup(c => c.GetAsync<PagedResult<GetTimelineQueryResponse>>(cacheKey))
                .ReturnsAsync(cachedResult);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(cachedResult.Items.Count, result.Items.Count);
            Assert.AreEqual(cachedResult.Items.First().Content, result.Items.First().Content);

            _cacheServiceMock.Verify(c => c.GetAsync<PagedResult<GetTimelineQueryResponse>>(cacheKey), Times.Once);
            _timelineRepositoryMock.Verify(r => r.GetTimelineAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Test]
        public async Task Handle_WithCacheMiss_ShouldFetchFromRepositoryAndCache()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var authorId = Guid.NewGuid();
            var pageNumber = 1;
            var pageSize = 10;
            var query = new GetTimelineQuery(userId, pageNumber, pageSize);
            var cacheKey = $"timeline:{userId}:page:{pageNumber}:size:{pageSize}";

            _cacheServiceMock.Setup(c => c.GetAsync<PagedResult<GetTimelineQueryResponse>>(cacheKey))
                .ReturnsAsync((PagedResult<GetTimelineQueryResponse>)null);

            var timelinesFromRepo = new List<Timeline>
            {
                new Timeline 
                { 
                    Id = Guid.NewGuid(), 
                    UserId = userId, 
                    TweetId = Guid.NewGuid(),
                    AuthorId = authorId,
                    Content = "Fresh tweet", 
                    CreatedAt = DateTime.UtcNow 
                }
            };

            _timelineRepositoryMock.Setup(r => r.GetTimelineAsync(userId, pageNumber, pageSize))
                .ReturnsAsync((timelinesFromRepo, 1L));

            var users = new List<User> { new User { Id = authorId, Username = "author" } };
            _userRepositoryMock.Setup(r => r.GetUsersByIdsAsync(It.IsAny<IEnumerable<Guid>>()))
                .ReturnsAsync(users);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Items.Count);
            Assert.AreEqual("Fresh tweet", result.Items.First().Content);

            _cacheServiceMock.Verify(c => c.GetAsync<PagedResult<GetTimelineQueryResponse>>(cacheKey), Times.Once);
            _cacheServiceMock.Verify(c => c.SetAsync(cacheKey, It.IsAny<PagedResult<GetTimelineQueryResponse>>(), TimeSpan.FromMinutes(5)), Times.Once);
            _timelineRepositoryMock.Verify(r => r.GetTimelineAsync(userId, pageNumber, pageSize), Times.Once);
        }
    }
}
