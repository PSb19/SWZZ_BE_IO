using Microsoft.AspNetCore.Identity;

namespace SWZZ_Backend.Models{
    public class ApplicationUser : IdentityUser{
        public string Name { get; set; }
        public string Surname { get; set; }
    }
}