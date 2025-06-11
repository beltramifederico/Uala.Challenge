using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Uala.Challenge.Api.Controllers;
using Uala.Challenge.Application.Tweets.Queries;
using Uala.Challenge.Domain.Common;

namespace Uala.Challenge.UnitTest.Controllers.Tweets
{
    [TestFixture]
    public class GetTimelineTests
    {
        private Mock<IMediator> _mediatorMock;
        private TweetsController _tweetsController;

        [SetUp]
        public void Setup()
        {
            _mediatorMock = new Mock<IMediator>();
            _tweetsController = new TweetsController(_mediatorMock.Object);
        }

        [Test]
        public async Task GetTimeline_ValidUserIdAndPagination_ShouldReturnOkWithPagedResultOfTimelineQueryResponse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var pageNumber = 1;
            var pageSize = 2;
            var totalItems = 5; // Example total items

            var timelineItems = new List<GetTimelineQueryResponse>
            {
                new GetTimelineQueryResponse { Id = Guid.NewGuid(), Content = "Tweet 1", UserId = userId, CreatedAt = DateTime.UtcNow },
                new GetTimelineQueryResponse { Id = Guid.NewGuid(), Content = "Tweet 2", UserId = userId, CreatedAt = DateTime.UtcNow.AddMinutes(-5) }
            };
            var expectedPagedResult = new PagedResult<GetTimelineQueryResponse>(timelineItems, totalItems, pageNumber, pageSize);

            _mediatorMock.Setup(m => m.Send(It.Is<GetTimelineQuery>(q => 
                                             q.UserId == userId && 
                                             q.PageNumber == pageNumber && 
                                             q.PageSize == pageSize), 
                                         It.IsAny<CancellationToken>()))
                         .ReturnsAsync(expectedPagedResult);

            // Act
            var actionResult = await _tweetsController.GetTimeline(userId, pageNumber, pageSize);

            // Assert
            Assert.That(actionResult, Is.InstanceOf<ActionResult<PagedResult<GetTimelineQueryResponse>>>());
            var okObjectResult = actionResult.Result as OkObjectResult;
            Assert.That(okObjectResult, Is.Not.Null);
            var actualPagedResult = okObjectResult.Value as PagedResult<GetTimelineQueryResponse>;
            Assert.That(actualPagedResult, Is.Not.Null);
            Assert.That(actualPagedResult.Items.Count, Is.EqualTo(timelineItems.Count));
            Assert.That(actualPagedResult.PageNumber, Is.EqualTo(pageNumber));
            Assert.That(actualPagedResult.PageSize, Is.EqualTo(pageSize));
            Assert.That(actualPagedResult.TotalItems, Is.EqualTo(totalItems));
            Assert.That(actualPagedResult.TotalPages, Is.EqualTo((int)Math.Ceiling(totalItems / (double)pageSize)));

            _mediatorMock.Verify(m => m.Send(It.Is<GetTimelineQuery>(q => 
                                              q.UserId == userId && 
                                              q.PageNumber == pageNumber && 
                                              q.PageSize == pageSize), 
                                          It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task GetTimeline_NoTweetsFound_ShouldReturnOkWithEmptyPagedResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var pageNumber = 1;
            var pageSize = 10;
            var expectedEmptyItems = new List<GetTimelineQueryResponse>();
            var expectedPagedResult = new PagedResult<GetTimelineQueryResponse>(expectedEmptyItems, 0, pageNumber, pageSize);

            _mediatorMock.Setup(m => m.Send(It.Is<GetTimelineQuery>(q => 
                                             q.UserId == userId &&
                                             q.PageNumber == pageNumber &&
                                             q.PageSize == pageSize),
                                         It.IsAny<CancellationToken>()))
                         .ReturnsAsync(expectedPagedResult);

            // Act
            var actionResult = await _tweetsController.GetTimeline(userId, pageNumber, pageSize);

            // Assert
            Assert.That(actionResult, Is.InstanceOf<ActionResult<PagedResult<GetTimelineQueryResponse>>>());
            var okObjectResult = actionResult.Result as OkObjectResult;
            Assert.That(okObjectResult, Is.Not.Null);
            var actualPagedResult = okObjectResult.Value as PagedResult<GetTimelineQueryResponse>;
            Assert.That(actualPagedResult, Is.Not.Null);
            Assert.That(actualPagedResult.Items.Any(), Is.False);
            Assert.That(actualPagedResult.TotalItems, Is.EqualTo(0));
            Assert.That(actualPagedResult.PageNumber, Is.EqualTo(pageNumber));
            Assert.That(actualPagedResult.PageSize, Is.EqualTo(pageSize));
            Assert.That(actualPagedResult.TotalPages, Is.EqualTo(0));
        }
    }
}