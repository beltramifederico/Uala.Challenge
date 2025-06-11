using MediatR;
using Uala.Challenge.Domain.Repositories;

namespace Uala.Challenge.Application.Users.Commands
{
    public class UnfollowUserCommandHandler(IUserRepository userRepository) : IRequestHandler<UnfollowUserCommand, Unit>
    {
        private readonly IUserRepository _userRepository = userRepository;

        public async Task<Unit> Handle(UnfollowUserCommand request, CancellationToken cancellationToken)
        {
            var follower = await _userRepository.Get(request.FollowerId, include: true, traking: true)
                ?? throw new ArgumentException("Follower not found");

            var followed = await _userRepository.Get(request.FollowedId, include: false, traking: false)
                ?? throw new ArgumentException("Followed user not found");

            var userToRemove = follower.Following?.FirstOrDefault(f => f.Id == followed.Id);

            if (userToRemove == null)
            {
                throw new ArgumentException($"User {follower.Id} to remove is not following user {followed.Id}");
            }

            follower.Following?.Remove(userToRemove);
            await _userRepository.UpdateAsync(follower);
            return default;
        }
    }
}
