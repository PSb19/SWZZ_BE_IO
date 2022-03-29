using System;
using System.Collections.Generic;
using SWZZ_Backend.Enums;

namespace SWZZ_Backend.Authorization
{
    public class TaskAuthorizeAttribute : Attribute
    {   
        public readonly SortedSet<AuthorizationRole> GroupRoles;

        public TaskAuthorizeAttribute(params AuthorizationRole[] groupRoles) 
        {
            GroupRoles = new SortedSet<AuthorizationRole>(groupRoles);
        }
    }
}