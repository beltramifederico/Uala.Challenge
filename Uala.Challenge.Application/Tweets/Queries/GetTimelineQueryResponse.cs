namespace Uala.Challenge.Application.Tweets.Queries
{
    public class GetTimelineQueryResponse
    {
        public Guid Id { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid UserId { get; set; }
    }
}
