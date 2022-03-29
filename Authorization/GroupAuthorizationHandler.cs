using System.Security.Claims;
using System.Threading.Tasks;
using SWZZ_Backend.Data;
using SWZZ_Backend.Models;
using SWZZ_Backend.Enums;

namespace SWZZ_Backend.Authorization
{
    public class GroupAuthorizationHandler : IGroupAuthorizationHandler
    {
        private ApplicationDbContext _dbContext;
        private readonly IGroupAuthorizationRequirement _authorizationRequirement;

        public GroupAuthorizationHandler(ApplicationDbContext context, IGroupAuthorizationRequirement authorizationRequirement)
        {
            _dbContext = context;
            _authorizationRequirement = authorizationRequirement;
        }  

        public async Task<bool> Authorize(ClaimsPrincipal user, int groupId, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            if (memberName == null)
                return false;

            AuthorizationRole? authorizationRole = _authorizationRequirement.GetAuthorizationRole(memberName);
            if (authorizationRole == null)
                return false;

            string userId = user.FindFirst(ClaimTypes.NameIdentifier).Value;
            bool authorized = false;

            GroupUser groupUser = await _dbContext.GroupUsers.FindAsync(userId, groupId);

            if (groupUser != null)
            {
                if (authorizationRole == AuthorizationRole.AnyMember || groupUser.Role == (Role)authorizationRole)
                    authorized = true;
            }

            return authorized;                 
        }
    }

    
    public interface IGroupAuthorizationHandler
    {
        public Task<bool> Authorize(ClaimsPrincipal user, int groupId, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "");
    }
}

