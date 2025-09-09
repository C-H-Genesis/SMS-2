using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DTOs
{
    public class UpdateFacultyDto
{
    [Required]
    public int FacultyId { get; set; }
    [Required, MaxLength(100)]
    public string FacultyName { get; set; } = string.Empty;

    [Required, MaxLength(5)]
    public string FacultyCode { get; set; } = string.Empty;
}
}