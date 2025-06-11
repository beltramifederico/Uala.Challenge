namespace Uala.Challenge.Domain.Entities;

public class Tweet
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Guid UserId { get; set; }
}
