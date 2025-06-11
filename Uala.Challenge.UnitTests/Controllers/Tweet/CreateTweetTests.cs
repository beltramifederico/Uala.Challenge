using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Uala.Challenge.Api.Controllers;
using Uala.Challenge.Application.Tweets.Commands;

namespace Uala.Challenge.UnitTest.Controllers.Tweets
{
    [TestFixture]
    public class CreateTweetTests
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
        public async Task CreateTweet_ValidCommand_ShouldReturnOkWithTweetDto()
        {
            // Arrange
            var createTweetCommand = new CreateTweetCommand { Content = "Test Tweet" , UserId = System.Guid.NewGuid() };
            var expectedTweetDto = new CreateTweetCommandResponse { Id = System.Guid.NewGuid(), Content = "Test Tweet", UserId = System.Guid.NewGuid(), CreatedAt = System.DateTime.UtcNow };

            _mediatorMock.Setup(m => m.Send(It.IsAny<CreateTweetCommand>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync(expectedTweetDto);

            // Act
            var actionResult = await _tweetsController.CreateTweet(createTweetCommand);

            // Assert
            Assert.That(actionResult, Is.InstanceOf<ActionResult<CreateTweetCommandResponse>>());
            var okObjectResult = actionResult.Result as OkObjectResult;
            Assert.That(okObjectResult, Is.Not.Null);
            Assert.That(okObjectResult.Value, Is.EqualTo(expectedTweetDto));
            _mediatorMock.Verify(m => m.Send(createTweetCommand, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}