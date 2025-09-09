using System.ComponentModel.DataAnnotations;


namespace DTOs
{
    // DTOs/UploadProfilePictureDto.cs
    public class UploadProfilePictureDto
    {
        /// <summary>
        /// The file input from the client.
        /// </summary>
        [Required]
        public IFormFile File { get; set; } = default!;
    }

}