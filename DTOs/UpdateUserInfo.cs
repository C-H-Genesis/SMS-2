
namespace DTOs
{
    public class UpdateUserInfo
{
    public required string UserName { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public string? RegNumber { get; set; }
    public required string PhoneNumber { get; set; }
    public List<string> Roles { get; set; } = new();

}

}