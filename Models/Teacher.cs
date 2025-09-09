
using System.ComponentModel.DataAnnotations.Schema;
using Models;

namespace Models
{
    public class Teacher : User
{
    [Column("Department")]
    public string? Department { get; set; }
     
}

}