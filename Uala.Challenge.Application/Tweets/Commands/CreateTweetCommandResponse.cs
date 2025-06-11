namespace Uala.Challenge.Application.Tweets.Commands
{
    public class CreateTweetCommandResponse
    {
        public Guid Id { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid UserId { get; set; }
        public string Username { get; set; }
    }
}
