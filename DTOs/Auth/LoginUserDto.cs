
namespace DTOs
{
    public class LoginUserDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public bool IsActive { get; set; }
    public List<RoleDto> Roles { get; set; } = new();
}

public class RoleDto
{
    public Guid RoleId { get; set; }
    public string RoleName { get; set; } = default!;
}

}