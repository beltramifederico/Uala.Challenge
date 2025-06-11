using Uala.Challenge.Domain.Events;

namespace Uala.Challenge.Application.Interfaces;

public interface IKafkaProducer
{
    Task PublishAsync<T>(string topic, T message) where T : class;
    Task PublishTweetCreatedAsync(TweetCreatedEvent tweetCreatedEvent);
}
