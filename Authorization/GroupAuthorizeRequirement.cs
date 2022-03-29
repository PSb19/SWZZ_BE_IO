using System.Linq;
using SWZZ_Backend.Enums;
using SWZZ_Backend.Controllers;
using System.Collections.Generic;

namespace SWZZ_Backend.Authorization
{
    public class GroupAuthorizationRequirement : IGroupAuthorizationRequirement
    {
        private readonly Dictionary<string, AuthorizationRole> AccessDictionary;

        public GroupAuthorizationRequirement()
        {
            AccessDictionary = new Dictionary<string, AuthorizationRole>();

            var roleSet = new SortedSet<AuthorizationRole>();
            foreach (var method in typeof(GroupController).GetMethods())
            {
                var authorizationAttribute = method.CustomAttributes.Where(attribute => attribute.AttributeType == typeof(GroupAuthorizeAttribute)).FirstOrDefault();
                if (authorizationAttribute != null )
                {
                    var authorizationRole = (AuthorizationRole)authorizationAttribute.ConstructorArguments.Single().Value;
                    AccessDictionary.Add(method.Name, authorizationRole);
                }
            }
        }

        public AuthorizationRole? GetAuthorizationRole(string memberName)
        {
            if (AccessDictionary.ContainsKey(memberName))
                return AccessDictionary[memberName];
            else
                return null;
        }
    }

    public interface IGroupAuthorizationRequirement
    {
        public AuthorizationRole? GetAuthorizationRole(string memberName);
    }
}



