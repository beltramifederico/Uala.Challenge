namespace Uala.Challenge.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public virtual ICollection<User> Following { get; set; } 
}
