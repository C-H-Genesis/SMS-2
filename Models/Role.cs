using Models;
using System.Text.Json.Serialization;


namespace Models
{
    public class Role
{
    public Guid RoleId { get; set; }
    public required string RoleName { get; set; }
   public ICollection<User> Users { get; set; } = new List<User>();
   public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    // Navigation property for many-to-many relationship
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
     
    
}

}