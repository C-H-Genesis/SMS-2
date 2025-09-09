using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace DTOs
{
    public class UploadAssignmentDto
    {
        [Required]
        [DataType(DataType.Upload)]
        public IFormFile File { get; set; } = default!;

        // After upload, these will be populated by the API response

        /// <summary>
        /// Primary key of the stored file record
        /// </summary>
        public Guid FileId { get; set; }

        /// <summary>
        /// Public URL to retrieve the uploaded file
        /// </summary>
        public string ContentType { get; set; } = string.Empty;

        public string FileUrl { get; set; } = string.Empty;
    }
}
