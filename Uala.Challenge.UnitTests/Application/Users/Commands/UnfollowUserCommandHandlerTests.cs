using MediatR;
using Moq;
using Uala.Challenge.Application.Users.Commands;
using Uala.Challenge.Domain.Entities;
using Uala.Challenge.Domain.Repositories;

namespace Uala.Challenge.UnitTests.Application.Users.Commands
{
    [TestFixture]
    public class UnfollowUserCommandHandlerTests
    {
        private Mock<IUserRepository> _userRepositoryMock;
        private UnfollowUserCommandHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _handler = new UnfollowUserCommandHandler(_userRepositoryMock.Object);
        }

        [Test]
        public async Task Handle_ValidUsersAndIsFollowing_ShouldRemoveFollowedAndSaveChanges()
        {
            // Arrange
            var followerId = Guid.NewGuid();
            var followedId = Guid.NewGuid();
            var command = new UnfollowUserCommand { FollowerId = followerId, FollowedId = followedId };

            var followed = new User { Id = followedId, Username = "followed" };
            var follower = new User { Id = followerId, Username = "follower", Following = new List<User> { followed } };

            _userRepositoryMock.Setup(r => r.Get(followerId, true, true)).ReturnsAsync(follower);
            _userRepositoryMock.Setup(r => r.Get(followedId, false, false)).ReturnsAsync(followed);
            _userRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.AreEqual(Unit.Value, result);
            Assert.IsFalse(follower.Following.Contains(followed));
            _userRepositoryMock.Verify(r => r.UpdateAsync(follower), Times.Once);
        }

        [Test]
        public void Handle_FollowerNotFound_ShouldThrowArgumentException()
        {
            // Arrange
            var followerId = Guid.NewGuid();
            var followedId = Guid.NewGuid();
            var command = new UnfollowUserCommand { FollowerId = followerId, FollowedId = followedId };

            _userRepositoryMock.Setup(r => r.Get(followerId, true, true)).ReturnsAsync((User)null);

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.AreEqual("Follower not found", ex.Message);
            _userRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
        }

        [Test]
        public void Handle_FollowedUserNotFound_ShouldThrowArgumentException()
        {
            // Arrange
            var followerId = Guid.NewGuid();
            var followedId = Guid.NewGuid();
            var command = new UnfollowUserCommand { FollowerId = followerId, FollowedId = followedId };

            var follower = new User { Id = followerId, Username = "follower", Following = new List<User>() };

            _userRepositoryMock.Setup(r => r.Get(followerId, true, true)).ReturnsAsync(follower);
            _userRepositoryMock.Setup(r => r.Get(followedId, false, false)).ReturnsAsync((User)null);

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(() => _handler.Handle(command, CancellationToken.None));
            Assert.AreEqual("Followed user not found", ex.Message);
            _userRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
        }

        [Test]
        public void Handle_UserIsNotFollowing_ShouldThrowException()
        {
            // Arrange
            var followerId = Guid.NewGuid();
            var followedId = Guid.NewGuid();
            var command = new UnfollowUserCommand { FollowerId = followerId, FollowedId = followedId };

            var follower = new User { Id = followerId, Username = "follower", Following = new List<User>() }; // Not following anyone
            var followed = new User { Id = followedId, Username = "followed" };

            _userRepositoryMock.Setup(r => r.Get(followerId, true, true)).ReturnsAsync(follower);
            _userRepositoryMock.Setup(r => r.Get(followedId, false, false)).ReturnsAsync(followed);

            // Act & Assert
            var ex = Assert.ThrowsAsync<Exception>(() => _handler.Handle(command, CancellationToken.None));
            Assert.AreEqual($"User {follower.Id} to remove is not following user {followed.Id}", ex.Message);
            _userRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Never);
        }
    }
}
