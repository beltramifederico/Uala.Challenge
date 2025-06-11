using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Uala.Challenge.Application.Tweets.Commands;
using Uala.Challenge.Application.Tweets.Queries;
using Uala.Challenge.Application.Users.Commands;
using Uala.Challenge.Domain.Common;
using Uala.Challenge.Domain.Entities;
using Uala.Challenge.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Uala.Challenge.Infrastructure.DAL.Contexts;
using MongoDB.Driver;
using Uala.Challenge.Application.Interfaces;

namespace Uala.Challenge.IntegrationTests.Controllers;

[TestFixture]
public class KafkaIntegrationTests : IntegrationTestBase
{
    private IMongoDatabase _mongoDatabase;
    private IMongoCollection<Timeline> _timelineCollection;

    [SetUp]
    public async Task SetUp()
    {
        using var scope = Factory.Services.CreateScope();
        _mongoDatabase = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
        _timelineCollection = _mongoDatabase.GetCollection<Timeline>("Timeline");
        
        // Clear timeline collection before each test
        await _timelineCollection.DeleteManyAsync(FilterDefinition<Timeline>.Empty);
    }

    [Test]
    public async Task GetTimeline_AfterKafkaProcessing_ShouldReturnTimelineFromMongoDB()
    {
        // Arrange - Create users with follow relationships
        var author = TestDataGenerator.GenerateUser("timelineauthor");
        var follower = TestDataGenerator.GenerateUser("timelinefollower");
        
        TestDataGenerator.SetupFollowRelationship(follower, author);
        
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();
        
        context.Users.AddRange(author, follower);
        await context.SaveChangesAsync();

        // Act - Create tweet
        var command = new CreateTweetCommand
        {
            UserId = author.Id,
            Content = "Timeline test tweet"
        };

        var createResponse = await Client.PostAsJsonAsync("/api/tweets", command);
        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Wait for Kafka processing
        await Task.Delay(2000);

        // Act - Get timeline
        var timelineResponse = await Client.GetAsync($"/api/tweets/timeline/{follower.Id}?pageNumber=1&pageSize=10");

        // Assert
        Assert.That(timelineResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        var timelineContent = await timelineResponse.Content.ReadAsStringAsync();
        var timeline = JsonSerializer.Deserialize<PagedResult<GetTimelineQueryResponse>>(timelineContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.That(timeline, Is.Not.Null);
        Assert.That(timeline.Items, Is.Not.Null);
        Assert.That(timeline.Items.Count, Is.EqualTo(1));
        Assert.That(timeline.Items.First().Content, Is.EqualTo("Timeline test tweet"));
        Assert.That(timeline.Items.First().Username, Is.EqualTo(author.Username));
    }
}
