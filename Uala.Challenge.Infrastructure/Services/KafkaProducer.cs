using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using Uala.Challenge.Application.Interfaces;
using Uala.Challenge.Domain.Events;

namespace Uala.Challenge.Infrastructure.Services;

public class KafkaProducer : IKafkaProducer, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private const string TweetCreatedTopic = "tweet-created";

    public KafkaProducer(IConfiguration configuration)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = configuration.GetConnectionString("Kafka") ?? "localhost:9092"
        };
        
        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync<T>(string topic, T message) where T : class
    {
        var messageJson = JsonSerializer.Serialize(message);
        
        await _producer.ProduceAsync(topic, new Message<string, string>
        {
            Key = Guid.NewGuid().ToString(),
            Value = messageJson
        });
    }

    public async Task PublishTweetCreatedAsync(TweetCreatedEvent tweetCreatedEvent)
    {
        await PublishAsync(TweetCreatedTopic, tweetCreatedEvent);
    }

    public void Dispose()
    {
        _producer?.Dispose();
    }
}
