using MediatR;
using Uala.Challenge.Application.Interfaces;
using Uala.Challenge.Domain.Common;
using Uala.Challenge.Domain.Repositories;
using Uala.Challenge.Domain.Services;

namespace Uala.Challenge.Application.Tweets.Queries
{
    public class GetTimelineQueryHandler : IRequestHandler<GetTimelineQuery, PagedResult<GetTimelineQueryResponse>>
    {
        private readonly ITimelineRepository _timelineRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICacheService _cacheService;

        public GetTimelineQueryHandler(
            ITimelineRepository timelineRepository, 
            IUserRepository userRepository,
            ICacheService cacheService)
        {
            _timelineRepository = timelineRepository;
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

            // Verificar que el usuario existe
            var user = await _userRepository.Get(request.UserId) 
                ?? throw new ArgumentException("User not found");

            // Obtener timeline entries del usuario
            var pagedTimelineResult = await _timelineRepository.GetTimelineAsync(request.UserId, request.PageNumber, request.PageSize);

            // Obtener información de los usuarios autores para incluir username
            var authorIds = pagedTimelineResult.timelines.Select(t => t.AuthorId).Distinct().ToList();
            var authors = new Dictionary<Guid, string>();
            
            foreach (var authorId in authorIds)
            {
                var author = await _userRepository.Get(authorId);
                if (author != null)
                {
                    authors[authorId] = author.Username;
                }
            }

            var timelineResponses = pagedTimelineResult.timelines.Select(t => new GetTimelineQueryResponse
            {
                Id = t.TweetId,
                Content = t.Content,
                CreatedAt = t.CreatedAt,
                UserId = t.AuthorId,
                Username = authors.GetValueOrDefault(t.AuthorId, "Unknown")
            }).ToList();

            var result = new PagedResult<GetTimelineQueryResponse>(timelineResponses, pagedTimelineResult.totalCount, request.PageNumber, request.PageSize);

            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

            return result;
        }
    }
}
