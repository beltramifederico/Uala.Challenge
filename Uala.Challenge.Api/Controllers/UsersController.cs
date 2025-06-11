using MediatR;
using Microsoft.AspNetCore.Mvc;
using Uala.Challenge.Application.Users.Commands;
using Uala.Challenge.Application.Users.Queries;

namespace Uala.Challenge.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost("/follow")]
    public async Task<IActionResult> Follow(FollowUserCommand command)
    {
        await _mediator.Send(command);
        return Ok();
    }

    [HttpDelete("/unfollow")]
    public async Task<IActionResult> Unfollow(UnfollowUserCommand command)
    {
         await _mediator.Send(command);
        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        var query = new GetAllUsersQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}
