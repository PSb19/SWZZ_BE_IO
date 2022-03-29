using System.Linq;
using SWZZ_Backend.Enums;
using SWZZ_Backend.Controllers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace SWZZ_Backend.Authorization
{
    public class TaskAuthorizationRequirement : ITaskAuthorizationRequirement
    {
        private readonly Dictionary<string, AuthorizationRole[]> AccessDictionary;

        public TaskAuthorizationRequirement()
        {
            AccessDictionary = new Dictionary<string, AuthorizationRole[]>();
            foreach (var method in typeof(TaskItemsController).GetMethods())
            {
                var authorizationAttribute = method.CustomAttributes.Where(attribute => attribute.AttributeType == typeof(TaskAuthorizeAttribute)).FirstOrDefault();
                if (authorizationAttribute != null )
                {
                    var arguments = (ReadOnlyCollection<CustomAttributeTypedArgument>)authorizationAttribute.ConstructorArguments.Single().Value;
                    var roles = new List<AuthorizationRole>();
                    foreach (var arg in arguments)
                    {
                        roles.Add((AuthorizationRole)arg.Value);
                    }
                    AccessDictionary.Add(method.Name, roles.ToArray());
                }
            }
        }

        public AuthorizationRole[] GetAuthorizationRoles(string memberName)
        {
            if (AccessDictionary.ContainsKey(memberName))
                return AccessDictionary[memberName];
            else
                return null;
        }
    }

    public interface ITaskAuthorizationRequirement
    {
        public AuthorizationRole[] GetAuthorizationRoles(string memberName);
    }
}

