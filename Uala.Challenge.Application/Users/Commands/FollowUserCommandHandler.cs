using MediatR;
using Uala.Challenge.Domain.Repositories;

namespace Uala.Challenge.Application.Users.Commands
{
    public class FollowUserCommandHandler(IUserRepository userRepository) : IRequestHandler<FollowUserCommand, Unit>
    {
        private readonly IUserRepository _userRepository = userRepository;

        public async Task<Unit> Handle(FollowUserCommand request, CancellationToken cancellationToken)
        {
            var follower = await _userRepository.Get(request.FollowerId, include: true, traking: true)
                ?? throw new ArgumentException("Follower not found");

            var followed = await _userRepository.Get(request.FollowedId, include: false, traking: false)
                ?? throw new ArgumentException("Followed user not found");

            if (follower.Following.Any(f => f.Id == followed.Id))
            {
                throw new ArgumentException($"User {follower.Id} is already following user {followed.Id}");
            }

            follower.Following.Add(followed);
            await _userRepository.UpdateAsync(follower);

            return Unit.Value;
        }
    }
}
