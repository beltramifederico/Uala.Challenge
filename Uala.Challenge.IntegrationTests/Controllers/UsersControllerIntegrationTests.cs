using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Uala.Challenge.Application.Users.Commands;
using Uala.Challenge.Application.Users.Queries;
using Uala.Challenge.Domain.Entities;
using Uala.Challenge.Infrastructure.DAL;
using Uala.Challenge.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Uala.Challenge.Infrastructure.DAL.Contexts;

namespace Uala.Challenge.IntegrationTests.Controllers;

[TestFixture]
public class UsersControllerIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task GetAllUsers_ShouldReturnUsersList()
    {
        // Arrange - Create some users directly in database
        var user1 = TestDataGenerator.GenerateUser("testuser1");
        var user2 = TestDataGenerator.GenerateUser("testuser2");
        
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();
        
        context.Users.AddRange(user1, user2);
        await context.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/users");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var users = JsonSerializer.Deserialize<List<GetAllUsersQueryResponse>>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.That(users, Is.Not.Null);
        Assert.That(users.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(users.Any(u => u.Username == "testuser1"), Is.True);
        Assert.That(users.Any(u => u.Username == "testuser2"), Is.True);
    }

    [Test]
    public async Task FollowUser_WithValidIds_ShouldCreateFollowRelationship()
    {
        // Arrange - Create users directly in database
        var follower = TestDataGenerator.GenerateUser("follower");
        var following = TestDataGenerator.GenerateUser("following");
        
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();
        
        context.Users.AddRange(follower, following);
        await context.SaveChangesAsync();

        var command = new FollowUserCommand
        {
            FollowerId = follower.Id,
            FollowedId = following.Id
        };

        // Act
        var response = await Client.PostAsJsonAsync("/follow", command);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Verify the relationship was created by checking database
        using var verifyScope = Factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<PostgresDbContext>();
        
        var updatedFollower = await verifyContext.Users
            .Include(u => u.Following)
            .FirstOrDefaultAsync(u => u.Id == follower.Id);

        Assert.That(updatedFollower, Is.Not.Null);
        Assert.That(updatedFollower.Following, Is.Not.Null);
        Assert.That(updatedFollower.Following.Any(f => f.Id == following.Id), Is.True);
    }

    [Test]
    public async Task UnfollowUser_WithExistingRelationship_ShouldRemoveFollowRelationship()
    {
        // Arrange - Create users and follow relationship directly in database
        var follower = TestDataGenerator.GenerateUser("unfollower");
        var following = TestDataGenerator.GenerateUser("unfollowing");
        
        // Set up follow relationship
        TestDataGenerator.SetupFollowRelationship(follower, following);
        
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();
        
        context.Users.AddRange(follower, following);
        await context.SaveChangesAsync();

        var unfollowCommand = new UnfollowUserCommand
        {
            FollowerId = follower.Id,
            FollowedId = following.Id
        };

        // Act
        var response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/unfollow")
        {
            Content = JsonContent.Create(unfollowCommand)
        });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Verify the relationship was removed by checking database
        using var verifyScope = Factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<PostgresDbContext>();
        
        var updatedFollower = await verifyContext.Users
            .Include(u => u.Following)
            .FirstOrDefaultAsync(u => u.Id == follower.Id);

        Assert.That(updatedFollower, Is.Not.Null);
        Assert.That(updatedFollower.Following?.Any(f => f.Id == following.Id) ?? false, Is.False);
    }

    [Test]
    public async Task FollowUser_WithNonExistentUsers_ShouldReturnBadRequest()
    {
        // Arrange
        var command = new FollowUserCommand
        {
            FollowerId = Guid.NewGuid(),
            FollowedId = Guid.NewGuid()
        };

        // Act
        var response = await Client.PostAsJsonAsync("/follow", command);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task UnfollowUser_WithNonExistentRelationship_ShouldReturnBadRequest()
    {
        // Arrange - Create users but no follow relationship
        var follower = TestDataGenerator.GenerateUser("nofollower");
        var following = TestDataGenerator.GenerateUser("nofollowing");
        
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();
        
        context.Users.AddRange(follower, following);
        await context.SaveChangesAsync();

        var unfollowCommand = new UnfollowUserCommand
        {
            FollowerId = follower.Id,
            FollowedId = following.Id
        };

        // Act
        var response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/unfollow")
        {
            Content = JsonContent.Create(unfollowCommand)
        });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }
}
