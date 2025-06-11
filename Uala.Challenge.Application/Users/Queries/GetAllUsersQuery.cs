using MediatR;
using Uala.Challenge.Domain.Entities;
using System.Collections.Generic;

namespace Uala.Challenge.Application.Users.Queries
{
    public class GetAllUsersQuery : IRequest<IEnumerable<GetAllUsersQueryResponse>> 
    {
    }
}
