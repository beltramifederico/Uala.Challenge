using MediatR;
using Uala.Challenge.Domain.Entities;
using Uala.Challenge.Domain.Repositories;
using Uala.Challenge.Domain.Services;

namespace Uala.Challenge.Application.Tweets.Commands
{
    public class CreateTweetCommandHandler : IRequestHandler<CreateTweetCommand, CreateTweetCommandResponse>
    {
        private readonly ITweetRepository _tweetRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICacheService _cacheService;

        public CreateTweetCommandHandler(
            ITweetRepository tweetRepository, 
            IUserRepository userRepository,
            ICacheService cacheService)
        {
            _tweetRepository = tweetRepository;
            _userRepository = userRepository;
            _cacheService = cacheService;
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