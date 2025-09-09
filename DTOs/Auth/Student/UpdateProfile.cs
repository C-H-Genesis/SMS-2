
namespace DTOs
{
    public class UpdateProfileDto
{
    public required string UserName { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public string? RegNumber { get; set; }
    public required string PhoneNumber { get; set; }
}

}