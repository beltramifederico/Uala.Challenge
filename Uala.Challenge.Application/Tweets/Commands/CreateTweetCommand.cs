using MediatR;

namespace Uala.Challenge.Application.Tweets.Commands;

public class CreateTweetCommand : IRequest<CreateTweetCommandResponse>
{
    public string Content { get; set; } = null!;

    public Guid UserId { get; set; }
}
