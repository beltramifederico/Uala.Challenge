using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Uala.Challenge.Application.Interfaces;
using Uala.Challenge.Domain.Entities;
using Uala.Challenge.Domain.Events;
using Uala.Challenge.Domain.Repositories;

namespace Uala.Challenge.Infrastructure.Services;

public class TweetCreatedConsumer : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TweetCreatedConsumer> _logger;
    private const string TweetCreatedTopic = "tweet-created";

    public TweetCreatedConsumer(IConfiguration configuration,
        IServiceProvider serviceProvider, 
        ILogger<TweetCreatedConsumer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        
        var config = new ConsumerConfig
        {
            BootstrapServers = configuration.GetConnectionString("Kafka") ?? "localhost:9092",
            GroupId = "timeline-consumer-group",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };
        
        _consumer = new ConsumerBuilder<string, string>(config).Build();
        _consumer.Subscribe(TweetCreatedTopic);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = _consumer.Consume(stoppingToken);
                
                if (consumeResult?.Message?.Value != null)
                {
                    var tweetCreatedEvent = JsonSerializer.Deserialize<TweetCreatedEvent>(consumeResult.Message.Value);
                    
                    if (tweetCreatedEvent != null)
                    {
                        await ProcessTweetCreatedEvent(tweetCreatedEvent);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing tweet created event");
                await Task.Delay(1000, stoppingToken); // Wait before retrying
            }
        }
    }

    private async Task ProcessTweetCreatedEvent(TweetCreatedEvent tweetCreatedEvent)
    {
        using var scope = _serviceProvider.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var timelineRepository = scope.ServiceProvider.GetRequiredService<ITimelineRepository>();

        try
        {
            // Obtener todos los followers del autor del tweet
            var followers = await userRepository.GetFollowersAsync(tweetCreatedEvent.AuthorId);
            
            // Crear entradas de timeline para cada follower
            var timelineEntries = followers.Select(follower => new Timeline
            {
                UserId = follower.Id,
                TweetId = tweetCreatedEvent.TweetId,
                AuthorId = tweetCreatedEvent.AuthorId,
                Content = tweetCreatedEvent.Content,
                CreatedAt = tweetCreatedEvent.CreatedAt
            }).ToList();

            // También crear una entrada para el propio autor
            timelineEntries.Add(new Timeline
            {
                UserId = tweetCreatedEvent.AuthorId,
                TweetId = tweetCreatedEvent.TweetId,
                AuthorId = tweetCreatedEvent.AuthorId,
                Content = tweetCreatedEvent.Content,
                CreatedAt = tweetCreatedEvent.CreatedAt
            });

            await timelineRepository.CreateTimelineEntriesAsync(timelineEntries);
            
            _logger.LogInformation("Created {Count} timeline entries for tweet {TweetId}", 
                timelineEntries.Count, tweetCreatedEvent.TweetId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating timeline entries for tweet {TweetId}", tweetCreatedEvent.TweetId);
            throw;
        }
    }

    public override void Dispose()
    {
        _consumer?.Close();
        _consumer?.Dispose();
        base.Dispose();
    }
}
