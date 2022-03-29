using SWZZ_Backend.Models;
using SWZZ_Backend.Enums;

namespace SWZZ_Backend.DTO
{
    public class RegisterForm
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }

        public RegisterForm() {}
    }

    public class LoginForm
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public bool RememberMe { get; set; }
        
        public LoginForm() {}
    }

    public class ChangePasswordForm
    {
        public string OldPassword { get; set; }
        public string NewPassword { get; set; }
        
        public ChangePasswordForm() {}
    }

    public class UserDTO
    {
        public string UserId { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }

        public UserDTO() {}

        public UserDTO(ApplicationUser user)
        {
            UserId = user.Id;
            Name = user.Name;
            Surname = user.Surname;
        }
    }

    public class UserAndRoleDTO
    {
        public UserDTO UserDTO { get; set; }
        public string Role { get; set; }

        public UserAndRoleDTO() {}
        public UserAndRoleDTO(UserDTO userDTO, Role role)
        {
            UserDTO = userDTO;
            Role = role.ToString();
        }
    }
}