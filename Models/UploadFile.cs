using Models;
using System.ComponentModel.DataAnnotations;

namespace Models
{

            public class UploadedFile
        {
            [Key]
            public required Guid FileId { get; set; }   // primary key
            public required string FileName    { get; set; }   // e.g. "homework1.pdf"
            public required string ContentType { get; set; }   // e.g. "application/pdf"
            public required string FileUrl { get; set; }
            public required byte[] Content { get; set; }   // "/uploads/abc123.pdf"
            public DateTime UploadedOn{ get; set; }

            
           
        }

    
}