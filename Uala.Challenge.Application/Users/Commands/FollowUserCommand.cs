using MediatR;

namespace Uala.Challenge.Application.Users.Commands
{
    public class FollowUserCommand : IRequest<Unit>
    {
        public Guid FollowedId { get; set; }

        public Guid FollowerId { get; set; }
    }
}
