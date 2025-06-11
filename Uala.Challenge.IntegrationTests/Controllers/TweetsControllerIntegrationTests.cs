using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Uala.Challenge.Application.Tweets.Commands;
using Uala.Challenge.Application.Tweets.Queries;
using Uala.Challenge.Application.Users.Commands;
using Uala.Challenge.Domain.Common;
using Uala.Challenge.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Uala.Challenge.Infrastructure.DAL;
using Uala.Challenge.Infrastructure.DAL.Contexts;

namespace Uala.Challenge.IntegrationTests.Controllers;

[TestFixture]
public class TweetsControllerIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task CreateTweet_WithValidData_ShouldReturnCreatedTweet()
    {
        // Arrange - Create user directly in database
        var user = TestDataGenerator.GenerateUser("tweetuser");
        
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();
        
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var command = new CreateTweetCommand
        {
            UserId = user.Id,
            Content = "This is my first tweet!"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/tweets", command);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var createdTweet = JsonSerializer.Deserialize<CreateTweetCommandResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.That(createdTweet, Is.Not.Null);
        Assert.That(createdTweet.Content, Is.EqualTo("This is my first tweet!"));
        Assert.That(createdTweet.UserId, Is.EqualTo(user.Id));
        Assert.That(createdTweet.Id, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public async Task CreateTweet_WithEmptyContent_ShouldReturnBadRequest()
    {
        // Arrange - Create user directly in database
        var user = TestDataGenerator.GenerateUser("tweetuser2");
        
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();
        
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var command = new CreateTweetCommand
        {
            UserId = user.Id,
            Content = ""
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/tweets", command);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task CreateTweet_WithNonExistentUser_ShouldReturnBadRequest()
    {
        // Arrange
        var command = new CreateTweetCommand
        {
            UserId = Guid.NewGuid(),
            Content = "This tweet should fail"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/tweets", command);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task GetTimeline_WithFollowedUsers_ShouldReturnPagedTweets()
    {
        // Arrange - Create users directly in database
        var user1 = TestDataGenerator.GenerateUser("user1");
        var user2 = TestDataGenerator.GenerateUser("user2");
        var follower = TestDataGenerator.GenerateUser("follower");
        
        // Set up follow relationships
        TestDataGenerator.SetupFollowRelationship(follower, user1);
        TestDataGenerator.SetupFollowRelationship(follower, user2);
        
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();
        
        context.Users.AddRange(user1, user2, follower);
        await context.SaveChangesAsync();

        // Create tweets
        await Client.PostAsJsonAsync("/api/tweets", new CreateTweetCommand
        {
            UserId = user1.Id,
            Content = "Tweet from user1"
        });

        await Client.PostAsJsonAsync("/api/tweets", new CreateTweetCommand
        {
            UserId = user2.Id,
            Content = "Tweet from user2"
        });

        await Client.PostAsJsonAsync("/api/tweets", new CreateTweetCommand
        {
            UserId = follower.Id,
            Content = "Tweet from follower"
        });

        // Act
        var response = await Client.GetAsync($"/api/tweets/timeline/{follower.Id}?pageNumber=1&pageSize=10");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var timeline = JsonSerializer.Deserialize<PagedResult<GetTimelineQueryResponse>>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.That(timeline, Is.Not.Null);
        Assert.That(timeline.Items, Is.Not.Null);
        Assert.That(timeline.Items.Count, Is.EqualTo(3)); // All 3 tweets should be in timeline
        Assert.That(timeline.TotalItems, Is.EqualTo(3));
        Assert.That(timeline.PageNumber, Is.EqualTo(1));
        Assert.That(timeline.PageSize, Is.EqualTo(10));

        // Verify tweets are from correct users
        var tweetContents = timeline.Items.Select(t => t.Content).ToList();
        Assert.That(tweetContents, Contains.Item("Tweet from user1"));
        Assert.That(tweetContents, Contains.Item("Tweet from user2"));
        Assert.That(tweetContents, Contains.Item("Tweet from follower"));
    }

    [Test]
    public async Task GetTimeline_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange - Create user directly in database
        var user = TestDataGenerator.GenerateUser("paginationuser");
        
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();
        
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Create multiple tweets
        for (int i = 1; i <= 15; i++)
        {
            await Client.PostAsJsonAsync("/api/tweets", new CreateTweetCommand
            {
                UserId = user.Id,
                Content = $"Tweet number {i}"
            });
        }

        // Act - Get first page
        var firstPageResponse = await Client.GetAsync($"/api/tweets/timeline/{user.Id}?pageNumber=1&pageSize=5");
        var firstPage = JsonSerializer.Deserialize<PagedResult<GetTimelineQueryResponse>>(
            await firstPageResponse.Content.ReadAsStringAsync(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Act - Get second page
        var secondPageResponse = await Client.GetAsync($"/api/tweets/timeline/{user.Id}?pageNumber=2&pageSize=5");
        var secondPage = JsonSerializer.Deserialize<PagedResult<GetTimelineQueryResponse>>(
            await secondPageResponse.Content.ReadAsStringAsync(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        Assert.That(firstPage, Is.Not.Null);
        Assert.That(firstPage.Items.Count, Is.EqualTo(5));
        Assert.That(firstPage.TotalItems, Is.EqualTo(15));
        Assert.That(firstPage.PageNumber, Is.EqualTo(1));
        Assert.That(firstPage.HasNextPage, Is.True);
        Assert.That(firstPage.HasPreviousPage, Is.False);

        Assert.That(secondPage, Is.Not.Null);
        Assert.That(secondPage.Items.Count, Is.EqualTo(5));
        Assert.That(secondPage.TotalItems, Is.EqualTo(15));
        Assert.That(secondPage.PageNumber, Is.EqualTo(2));
        Assert.That(secondPage.HasNextPage, Is.True);
        Assert.That(secondPage.HasPreviousPage, Is.True);
    }

    [Test]
    public async Task GetTimeline_WithNonExistentUser_ShouldReturnBadRequest()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/tweets/timeline/{nonExistentUserId}?pageNumber=1&pageSize=10");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
}
