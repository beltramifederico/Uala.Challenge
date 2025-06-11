using MediatR;
using Microsoft.AspNetCore.Mvc;
using Uala.Challenge.Application.Tweets.Commands;
using Uala.Challenge.Application.Tweets.Queries;
using Uala.Challenge.Domain.Common;

namespace Uala.Challenge.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TweetsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TweetsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<CreateTweetCommandResponse>> CreateTweet([FromBody] CreateTweetCommand command)
    {
        try
        {
            var tweet = await _mediator.Send(command);
            return Ok(tweet);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("timeline/{userId:guid}")]
    public async Task<ActionResult<PagedResult<GetTimelineQueryResponse>>> GetTimeline(Guid userId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var tweets = await _mediator.Send(new GetTimelineQuery(userId, pageNumber, pageSize));
        return Ok(tweets);
    }
}
