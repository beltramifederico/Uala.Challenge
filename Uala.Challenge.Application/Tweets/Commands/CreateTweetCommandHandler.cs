using MediatR;
using Uala.Challenge.Application.Interfaces;
using Uala.Challenge.Domain.Entities;
using Uala.Challenge.Domain.Events;
using Uala.Challenge.Domain.Repositories;
using Uala.Challenge.Domain.Services;

namespace Uala.Challenge.Application.Tweets.Commands
{
    public class CreateTweetCommandHandler : IRequestHandler<CreateTweetCommand, CreateTweetCommandResponse>
    {
        private readonly ITweetRepository _tweetRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICacheService _cacheService;
        private readonly IKafkaProducer _kafkaProducer;

        public CreateTweetCommandHandler(
            ITweetRepository tweetRepository, 
            IUserRepository userRepository,
            ICacheService cacheService,
            IKafkaProducer kafkaProducer)
        {
            _tweetRepository = tweetRepository;
            _userRepository = userRepository;
            _cacheService = cacheService;
            _kafkaProducer = kafkaProducer;
        }

        public async Task<CreateTweetCommandResponse> Handle(CreateTweetCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.Get(request.UserId)
                ?? throw new ArgumentException("User not found");

            var tweet = new Tweet
            {
                Content = request.Content,
                UserId = request.UserId,
                CreatedAt = DateTime.UtcNow
            };

            await _tweetRepository.AddAsync(tweet);

            // Publicar evento en Kafka para generar timeline entries
            var tweetCreatedEvent = new TweetCreatedEvent(
                tweet.Id,
                tweet.UserId,
                tweet.Content,
                tweet.CreatedAt
            );
            
            await _kafkaProducer.PublishTweetCreatedAsync(tweetCreatedEvent);

            await InvalidateFollowersTimelines(user);

            return new CreateTweetCommandResponse
            {
                Id = tweet.Id,
                Content = tweet.Content,
                CreatedAt = tweet.CreatedAt,
                UserId = tweet.UserId,
                Username = user.Username
            };
        }

        private async Task InvalidateFollowersTimelines(User user)
        {
            await _cacheService.RemovePatternAsync($"timeline:{user.Id}:*");
        }
    }
}