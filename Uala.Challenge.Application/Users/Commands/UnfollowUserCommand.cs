using MediatR;

namespace Uala.Challenge.Application.Users.Commands
{
    public class UnfollowUserCommand : IRequest<Unit>
    {
        public Guid FollowerId { get; set; }
        public Guid FollowedId { get; set; }
    }
}
