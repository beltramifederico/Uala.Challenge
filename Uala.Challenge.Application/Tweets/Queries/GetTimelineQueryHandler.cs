using MediatR;
using Uala.Challenge.Domain.Repositories;

namespace Uala.Challenge.Application.Tweets.Queries
{
    using Uala.Challenge.Domain.Common;
    using Uala.Challenge.Domain.Services;

    public class GetTimelineQueryHandler : IRequestHandler<GetTimelineQuery, PagedResult<GetTimelineQueryResponse>>
    {
        private readonly ITweetRepository _tweetRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICacheService _cacheService;

        public GetTimelineQueryHandler(
            ITweetRepository tweetRepository, 
            IUserRepository userRepository,
            ICacheService cacheService)
        {
            _tweetRepository = tweetRepository;
            _userRepository = userRepository;
            _cacheService = cacheService;
        }

        public async Task<PagedResult<GetTimelineQueryResponse>> Handle(GetTimelineQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"timeline:{request.UserId}:page:{request.PageNumber}:size:{request.PageSize}";
            
            var cachedResult = await _cacheService.GetAsync<PagedResult<GetTimelineQueryResponse>>(cacheKey);
            if (cachedResult != null)
            {
                return cachedResult;
            }

            var user = await _userRepository.Get(request.UserId, include: true) 
                ?? throw new ArgumentException("User not found");

            var followingIds = user.Following != null ?
                user.Following.Select(f => f.Id) :
                new List<Guid>();

            var pagedTweetsResult = await _tweetRepository.
                GetTimelineAsync(followingIds.Append(user.Id), request.PageNumber, request.PageSize);

            var tweetResponses = pagedTweetsResult.Item1.Select(t => new GetTimelineQueryResponse
            {
                Id = t.Id,
                Content = t.Content,
                CreatedAt = t.CreatedAt,
                UserId = t.UserId
            }).ToList();

            var result = new PagedResult<GetTimelineQueryResponse>(tweetResponses, pagedTweetsResult.Item2, request.PageNumber, request.PageSize);

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

            return result;
        }
    }

}
