using Bogus;
using Uala.Challenge.Domain.Entities;

namespace Uala.Challenge.IntegrationTests.Infrastructure;

public static class TestDataGenerator
{
    private static readonly Faker<User> UserFaker = new Faker<User>()
        .RuleFor(u => u.Id, f => Guid.NewGuid())
        .RuleFor(u => u.Username, f => f.Internet.UserName())
        .RuleFor(u => u.Following, f => new List<User>());

    private static readonly Faker<Tweet> TweetFaker = new Faker<Tweet>()
        .RuleFor(t => t.Id, f => Guid.NewGuid())
        .RuleFor(t => t.Content, f => f.Lorem.Sentence(10, 20))
        .RuleFor(t => t.CreatedAt, f => f.Date.Recent(30))
        .RuleFor(t => t.UserId, f => Guid.NewGuid());

    public static User GenerateUser(string? username = null)
    {
        var user = UserFaker.Generate();
        if (!string.IsNullOrEmpty(username))
        {
            user.Username = username;
        }
        return user;
    }

    public static List<User> GenerateUsers(int count)
    {
        return UserFaker.Generate(count);
    }

    public static Tweet GenerateTweet(Guid? userId = null, string? content = null)
    {
        var tweet = TweetFaker.Generate();
        if (userId.HasValue)
        {
            tweet.UserId = userId.Value;
        }
        if (!string.IsNullOrEmpty(content))
        {
            tweet.Content = content;
        }
        return tweet;
    }

    public static List<Tweet> GenerateTweets(int count, Guid? userId = null)
    {
        var tweets = TweetFaker.Generate(count);
        if (userId.HasValue)
        {
            tweets.ForEach(t => t.UserId = userId.Value);
        }
        return tweets;
    }

    public static void SetupFollowRelationship(User follower, User following)
    {
        follower.Following ??= new List<User>();
        
        if (!follower.Following.Contains(following))
        {
            follower.Following.Add(following);
        }
        

    }
}
