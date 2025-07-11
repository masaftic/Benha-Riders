using Vogen;

namespace BenhaScooters.Domain;

[ValueObject<int>]
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

    public UserRole(RoleName name)
    {
        Name = name;
    }
}
