namespace WebUI.Models
{
    public class UserRoleAssignViewModel
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public bool RoleExist { get; set; }
    }

    public class UserWithRolesViewModel
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Image { get; set; }
        public List<string> Roles { get; set; } = new();
        public bool IsLockedOut { get; set; }
    }
}
