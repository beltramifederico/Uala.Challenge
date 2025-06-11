using System.Collections.Generic;
using Uala.Challenge.Domain.Entities;

namespace Uala.Challenge.Application.Users.Queries
{
    public class GetAllUsersQueryResponse
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
    }
}
