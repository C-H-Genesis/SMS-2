using Models;

namespace Models
{
    public class RolePermission
{
    public Guid RoleId { get; set; }
    public int PermissionId { get; set; }
    
    // Navigation properties to Role and Permission
    public Role? Role { get; set; }
    public Permission? Permission { get; set; }
}


}