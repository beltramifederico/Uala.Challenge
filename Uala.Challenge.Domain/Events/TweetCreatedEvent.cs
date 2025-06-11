namespace Uala.Challenge.Domain.Events;

public class TweetCreatedEvent
{
    public Guid TweetId { get; set; }
    public Guid AuthorId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    
    public TweetCreatedEvent(Guid tweetId, Guid authorId, string content, DateTime createdAt)
    {
        TweetId = tweetId;
        AuthorId = authorId;
        Content = content;
        CreatedAt = createdAt;
    }
}
