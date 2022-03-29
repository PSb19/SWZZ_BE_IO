using SWZZ_Backend.Enums;

namespace SWZZ_Backend.DTO
{
    public class TaskPermissions
    {
        public bool CanEditAttributes { get; set; }
        public bool CanAssignOthers { get; set; }
        public bool CanRenounce { get; set; }
        public bool CanDelete { get; set; }

        public TaskPermissions(Role role)
        {
            if(role == Role.Administrator){
                CanEditAttributes = true;
                CanAssignOthers = true;
                CanRenounce = true;
                CanDelete = true;
            }else if(role == Role.PrivilegedUser){
                CanEditAttributes = false;
                CanAssignOthers = false;
                CanRenounce = true;
                CanDelete = false;
            }else{
                CanEditAttributes = false;
                CanAssignOthers = false;
                CanRenounce = false;
                CanDelete = false;
            }
        }
    }

    public class GroupPermissions
    {
        public bool CanEditAttributes { get; set; }
        public bool CanGetCode { get; set; }
        public bool CanResetCode { get; set; }
        public bool CanChangeUserRole { get; set; }
        public bool CanRemoveUser { get; set; }
        public bool CanDelete { get; set; }

        public GroupPermissions(Role role) 
        {
            bool isAdmin = (role == Role.Administrator);
            CanEditAttributes = isAdmin;
            CanGetCode = isAdmin;
            CanResetCode = isAdmin;
            CanChangeUserRole = isAdmin;
            CanRemoveUser = isAdmin;
            CanDelete = isAdmin;
        }
    }
}