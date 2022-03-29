using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using SWZZ_Backend.Data;
using SWZZ_Backend.Enums;

namespace SWZZ_Backend.Authorization
{
    public class TaskAuthorizationHandler : ITaskAuthorizationHandler
    {
        private ApplicationDbContext _dbContext;
        private readonly ITaskAuthorizationRequirement _authorizationRequirement;

        public TaskAuthorizationHandler(ApplicationDbContext context, ITaskAuthorizationRequirement authorizationRequirement)
        {
            _dbContext = context;
            _authorizationRequirement = authorizationRequirement;
        }  

        public async Task<bool> Authorize(ClaimsPrincipal user, int taskId, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            if (memberName == null)
                return false;

            AuthorizationRole[]  authorizationRoles = _authorizationRequirement.GetAuthorizationRoles(memberName);
            if (authorizationRoles.Length == 0)
                return false;

            string userId = user.FindFirst(ClaimTypes.NameIdentifier).Value;
            bool authorized = false;

            var task = await _dbContext.TaskItems.FindAsync(taskId);
            if (task == null)
                return false;

            var groupUser = await _dbContext.GroupUsers.FindAsync(userId, task.GroupId);

            if (groupUser != null)
            {
                if (authorizationRoles.Contains(AuthorizationRole.AnyMember) || 
                    authorizationRoles.Contains((AuthorizationRole)groupUser.Role) ||
                    (authorizationRoles.Contains(AuthorizationRole.Owner) && userId == task.CommissionerName))
                {
                    authorized = true;
                }
            }

            return authorized;                 
        }
    }
    
    public interface ITaskAuthorizationHandler
    {
        public Task<bool> Authorize(ClaimsPrincipal user, int taskId, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "");
    }
}

