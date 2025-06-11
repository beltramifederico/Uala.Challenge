using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Uala.Challenge.Api.Controllers;
using Uala.Challenge.Application.Users.Commands;

namespace Uala.Challenge.UnitTest.Controllers.Users
{
    [TestFixture]
    public class FollowTests
    {
        private Mock<IMediator> _mediatorMock;
        private UsersController _usersController;

        [SetUp]
        public void Setup()
        {
            _mediatorMock = new Mock<IMediator>();
            _usersController = new UsersController(_mediatorMock.Object);
        }

        [Test]
        public async Task Follow_ValidCommand_ShouldReturnOkResult()
        {
            // Arrange
            var followCommand = new FollowUserCommand();

            _mediatorMock.Setup(m => m.Send(It.IsAny<FollowUserCommand>(), It.IsAny<CancellationToken>()))
                         .Returns(Task.FromResult(Unit.Value));

            // Act
            var actionResult = await _usersController.Follow(followCommand);

            // Assert
            Assert.That(actionResult, Is.InstanceOf<OkResult>());
            _mediatorMock.Verify(m => m.Send(followCommand, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Follow_MediatorThrowsException_ShouldPropagateException()
        {
            // Arrange
            var followCommand = new FollowUserCommand();
            var expectedException = new System.Exception("Mediator error");
            _mediatorMock.Setup(m => m.Send(It.IsAny<FollowUserCommand>(), It.IsAny<CancellationToken>()))
                         .ThrowsAsync(expectedException);

            // Act & Assert
            var ex = Assert.ThrowsAsync<System.Exception>(async () => await _usersController.Follow(followCommand));
            Assert.That(ex, Is.Not.Null);
            Assert.That(ex.Message, Is.EqualTo(expectedException.Message));
            _mediatorMock.Verify(m => m.Send(followCommand, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}