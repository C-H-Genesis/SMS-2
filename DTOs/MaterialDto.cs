
namespace DTOs
{
    public class MaterialDto
{
    public required string Title { get; set; }
    public required string FileUrl { get; set; }
    public Guid CourseId { get; set; }
}
}