using Vogen;

namespace BenhaScooters.Domain;

[ValueObject<Guid>]
public partial struct UserRoleId;


public enum RoleName
{
    Rider,
    Admin, 
    Driver
}



public class UserRole
{
    public UserRoleId Id { get; private set; }
    public UserId UserId { get; private set; }
    public RoleName Name { get; private set; }

    public UserRole(UserId userId, RoleName name)
    {
        Id = UserRoleId.From(Guid.NewGuid());
        UserId = userId;
        Name = name;
    }
}
