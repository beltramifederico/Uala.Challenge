using MediatR;

namespace Uala.Challenge.Application.Tweets.Queries;

using Uala.Challenge.Domain.Common;

public class GetTimelineQuery : IRequest<PagedResult<GetTimelineQueryResponse>>
{
    public Guid UserId { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }

    public GetTimelineQuery(Guid userId, int pageNumber, int pageSize)
    {
        UserId = userId;
        PageNumber = pageNumber > 0 ? pageNumber : 1;
        PageSize = pageSize > 0 ? pageSize : 10; // Default page size to 10 if not specified or invalid
    }
}
