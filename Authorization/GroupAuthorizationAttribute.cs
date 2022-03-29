using System;
using SWZZ_Backend.Enums;

namespace SWZZ_Backend.Authorization
{
    public class GroupAuthorizeAttribute : Attribute
    {   
        public readonly AuthorizationRole GroupRole;

        public GroupAuthorizeAttribute(AuthorizationRole groupRole) 
        {
            GroupRole = groupRole;
        }
    }
}