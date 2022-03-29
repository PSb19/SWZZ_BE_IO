using SWZZ_Backend.Models;
using SWZZ_Backend.Enums;
using System;

namespace SWZZ_Backend.DTO
{
    public class GroupUserDTO
    {
        public string UserId {get; set;}
        public int GroupId {get; set;}
        public string UserRole {get; set;}

        public GroupUserDTO() {}

        public void CopyToGroupUser(GroupUser groupUser)
        {
            Role role;
            try
            {
                role = Enum.Parse<Role>(UserRole);
            }
            catch (Exception ex)
            {
                if (ex is ArgumentException || ex is OverflowException)
                {
                    throw new ArgumentException(String.Format("'{0}' is not Role enum member", UserRole), ex);
                }
                else throw;
            }
            groupUser.UserId = UserId;
            groupUser.GroupId = GroupId;
            groupUser.Role = role;
        }
    }
}