namespace Uala.Challenge.Domain.Entities;

public class Timeline
{
    public Guid Id { get; set; }
    
    public Guid UserId { get; set; }
    
    public Guid TweetId { get; set; }
    
    public Guid AuthorId { get; set; } 
    
    public DateTime CreatedAt { get; set; }
    
    public string Content { get; set; } = string.Empty;
    
    public Timeline()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }
}
