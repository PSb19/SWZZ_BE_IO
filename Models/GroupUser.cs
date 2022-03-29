using SWZZ_Backend.Enums;

namespace SWZZ_Backend.Models{
    public class GroupUser {
        public string UserId  { get; set; }
        public int GroupId { get; set; }
        public Role Role  { get; set; }
    }
}