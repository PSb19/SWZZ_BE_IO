using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;

using SWZZ_Backend.Data;
using SWZZ_Backend.DTO;
using SWZZ_Backend.Models;
using SWZZ_Backend.Authorization;
using SWZZ_Backend.Enums;

namespace SWZZ_Backend.Controllers
{
    using static AuthorizationRole;
    [Authorize]
    [ApiController]
    public class GroupController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IGroupAuthorizationHandler _authorizationHandler;

        public GroupController(ApplicationDbContext dbContext, IGroupAuthorizationHandler authorizationHandler)
        {
            _dbContext = dbContext;
            _authorizationHandler = authorizationHandler;
        }

        [HttpPost("group")]
        public async Task<ActionResult<int>> CreateGroup(GroupDTO groupDTO)
        {
            string code;
            do
            {
                code = GenerateCode();
            } while(_dbContext.Groups.Any(g => g.Code == code));

            var group = groupDTO.toGroup(code);
            _dbContext.Groups.Add(group);

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                if (ex is DbUpdateException || ex is DbUpdateConcurrencyException)
                    return StatusCode(500); // 500 - Internal server error- Informs client that error occured, but not due to incorrect request
                else
                    throw;
            }

            var groupUser = new GroupUser() {UserId = CallerId(), GroupId = group.Id, Role = Enums.Role.Administrator };
            _dbContext.GroupUsers.Add(groupUser);

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Reverts changes if any exception occured
                _dbContext.Groups.Remove(group);
                _dbContext.GroupUsers.Remove(groupUser);
                await _dbContext.SaveChangesAsync();

                if (ex is DbUpdateException || ex is DbUpdateConcurrencyException)
                    return StatusCode(500); // 500 - Internal server error- Informs client that error occured, but not due to incorrect request
                else
                    throw;
            }
            return group.Id;
        }
        
        [HttpGet("group")]
        [GroupAuthorize(AnyMember)]
        public async Task<ActionResult<GroupAndPermissionsDTO>> GetGroupById(int groupId)
        {
            if (!(await _authorizationHandler.Authorize(User, groupId)))
                return StatusCode(403);

            Group group = await _dbContext.Groups.FindAsync(groupId);
            GroupUser groupUser = await _dbContext.GroupUsers.FindAsync(CallerId(), groupId);

            if (group == null || groupUser == null)
                return BadRequest();

            var groupAndPermissionsDTO = new GroupAndPermissionsDTO(
                new GroupDTO(group),
                new GroupPermissions(groupUser.Role)
            );
            return groupAndPermissionsDTO;
        }

        [HttpDelete("group")]
        [GroupAuthorize(Administrator)]
        public async Task<IActionResult> DeleteGroup(int groupId)
        {
            if (!(await _authorizationHandler.Authorize(User, groupId)))
                return NotAuthorized();
            // It'd be better to deal with it using cascade delete bahavior in ApplicationContext, 
            // deleting it manually not to make changes to database
            try
            {
                _dbContext.Groups.RemoveRange( _dbContext.Groups.Where(g => g.Id == groupId));
                _dbContext.GroupUsers.RemoveRange(_dbContext.GroupUsers.Where(u => u.GroupId == groupId));
                _dbContext.TaskItems.RemoveRange(_dbContext.TaskItems.Where(t => t.GroupId == groupId));
            }
            catch (ArgumentNullException)
            {
                return BadRequest();
            }

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                if (ex is DbUpdateException || ex is DbUpdateConcurrencyException)
                    return StatusCode(500); // 500 - Internal server error- Informs client that error occured, but not due to incorrect request
                else
                    throw;
            }
            return Ok();
        }

        [HttpPut("group/attributes")]
        [GroupAuthorize(Administrator)]
        public async Task<IActionResult> EditGroupAttributes(GroupDTO groupDTO)
        {
            if (!(await _authorizationHandler.Authorize(User, groupDTO.GroupId)))
                return NotAuthorized();

            Group group;
            try
            {
                group = await _dbContext.Groups.Where(g => g.Id == groupDTO.GroupId).FirstAsync();
            }
            catch (Exception ex)
            {
                if (ex is ArgumentNullException || ex is InvalidOperationException)
                    return BadRequest();
                else 
                    throw;
            }

            group.Icon = groupDTO.Icon;
            group.Name = groupDTO.Name;
            group.Description = groupDTO.Description;
            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                if (ex is DbUpdateException || ex is DbUpdateConcurrencyException)
                    return StatusCode(500); // 500 - Internal server error- Informs client that error occured, but not due to incorrect request
                else
                    throw;
            }
            return Ok();
        }

        [HttpGet("group/code")]
        [GroupAuthorize(Administrator)]
        public async Task<ActionResult<string>> PeekGroupCode(int groupId)
        {
            if (!(await _authorizationHandler.Authorize(User, groupId)))
                return NotAuthorized();

            Group group;
            try
            {
                group = await _dbContext.Groups.Where(g => g.Id == groupId).FirstAsync();
            }
            catch (Exception ex)
            {
                if (ex is ArgumentNullException || ex is InvalidOperationException)
                    return BadRequest();
                else 
                    throw;
            }
            return group.Code;
        }

        [HttpPut("group/code")]
        [GroupAuthorize(Administrator)]
        public async Task<ActionResult<string>> ResetGroupCode(int groupId)
        {
            if (!(await _authorizationHandler.Authorize(User, groupId)))
                return NotAuthorized();

            Group group;
            try
            {
                group = await _dbContext.Groups.Where(g => g.Id == groupId).FirstAsync();
            }
            catch (Exception ex)
            {
                if (ex is ArgumentNullException || ex is InvalidOperationException)
                    return BadRequest();
                else 
                    throw;
            }

            do
            {
                group.Code = GenerateCode();
            } while(_dbContext.Groups.Any(g => g.Code == group.Code));

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                if (ex is DbUpdateException || ex is DbUpdateConcurrencyException)
                    return StatusCode(500); // 500 - Internal server error- Informs client that error occured, but not due to incorrect request
                else
                    throw;
            }
            return group.Code;
        }

        [HttpPut("group/user")]
        [GroupAuthorize(Administrator)]
        public async Task<IActionResult> ChangeUserRole(GroupUserDTO groupUserDTO)
        {
            if (!(await _authorizationHandler.Authorize(User, groupUserDTO.GroupId)))
                return NotAuthorized();

            // Check if user to change is not the caller
            if (groupUserDTO.UserId == CallerId())
                return BadRequest();

            GroupUser groupUser;
            try
            {
                groupUser = await _dbContext.GroupUsers.Where(u => 
                    u.GroupId == groupUserDTO.GroupId && 
                    u.UserId == groupUserDTO.UserId).FirstAsync();
            }
            catch (Exception ex)
            {
                if (ex is ArgumentNullException || ex is InvalidOperationException)
                    return BadRequest();
                else 
                    throw;
            }
            
            try
            {
                groupUserDTO.CopyToGroupUser(groupUser);
            }
            catch (ArgumentException) 
            {
                return BadRequest();
            }        

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                if (ex is DbUpdateException || ex is DbUpdateConcurrencyException)
                    return StatusCode(500); // 500 - Internal server error- Informs client that error occured, but not due to incorrect request
                else
                    throw;
            }
            return Ok();
        }
        
        [HttpDelete("group/user")]
        [GroupAuthorize(Administrator)]
        public async Task<IActionResult> RemoveUserFromGroup(GroupUserDTO groupUserDTO)
        {
            if (!(await _authorizationHandler.Authorize(User, groupUserDTO.GroupId)))
                return NotAuthorized();

            try
            {
                GroupUser groupUser = await _dbContext.GroupUsers
                    .Where(u => u.GroupId == groupUserDTO.GroupId && u.UserId == groupUserDTO.UserId).FirstOrDefaultAsync();
                if (groupUser == null)  // No such user in a group
                    return BadRequest();
                _dbContext.GroupUsers.Remove(groupUser);
                //Get all task of user in that group
                var unassignedTasks = await _dbContext.TaskItems
                    .Where( t => t.CommissioneeName.Equals(groupUserDTO.UserId) && t.GroupId == groupUserDTO.GroupId).ToListAsync();
                foreach (var taskItem in unassignedTasks){ //Unassign user from tasks
                    taskItem.CommissioneeName=null;
                }
            }
            catch (ArgumentNullException)
            {
                return BadRequest();
            }

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                if (ex is DbUpdateException || ex is DbUpdateConcurrencyException)
                    return StatusCode(500); // 500 - Internal server error- Informs client that error occured, but not due to incorrect request
                else
                    throw;
            }
            return Ok();
        }

        [HttpGet("group/tasks")]
        [GroupAuthorize(AnyMember)]
        public async Task<ActionResult<IEnumerable<TaskAndPermissionsDTO>>> GetTaskItemsByGroupId(int groupId)
        {
            if (!(await _authorizationHandler.Authorize(User, groupId)))
                return NotAuthorized();

            //Returns list of all Task Items. 
            List<TaskItem> taskItems;
            try
            {
                 taskItems = await _dbContext.TaskItems.Where(t => t.GroupId == groupId).ToListAsync();
            }
            catch (ArgumentNullException)
            {
                return BadRequest();
            }
            //Gets user NameIdentifier of current User
            var userIdentifier = this.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            //Gets user Role in current group:
            GroupUser groupRole = await _dbContext.GroupUsers.FindAsync(userIdentifier, groupId);
            //Create list of type TaskAndPermissionsDTO to pass as response
            var TaskAndPermissionsDTOList = new List<TaskAndPermissionsDTO>();
            foreach (var taskItem in taskItems)
            {
                //Set permission level accordingly to role in the group.
                TaskPermissions permission = new TaskPermissions(groupRole.Role);
                if(taskItem.CommissionerName == userIdentifier){
                    //full controll over task.
                    permission = new TaskPermissions(Enums.Role.Administrator);
                }
                TaskAndPermissionsDTOList.Add(new TaskAndPermissionsDTO(
                    new TaskItemDTO(taskItem),
                    permission
                ));
            }
            return TaskAndPermissionsDTOList;
        }

        [HttpGet("group/users")]
        [GroupAuthorize(AnyMember)]
        public async Task<ActionResult<IEnumerable<UserAndRoleDTO>>> GetUsersByGroupId(int groupId)
        {
            if (!(await _authorizationHandler.Authorize(User, groupId)))
                return NotAuthorized();

            List<UserAndRoleDTO> userAndRoleList;
            try
            {
                 userAndRoleList = await _dbContext.GroupUsers
                .Where(gUser => gUser.GroupId == groupId)
                .Join(_dbContext.ApplicationUsers,
                    gUser => gUser.UserId,
                    user => user.Id,
                    (gUser, user) => new UserAndRoleDTO(
                        new UserDTO() {
                            UserId = user.Id,
                            Name = user.Name,
                            Surname = user.Surname,
                        },
                        gUser.Role
                )).ToListAsync();
            }
            catch (ArgumentNullException)
            {
                return BadRequest();
            }
            // Get list of all users and their roles in them
            return userAndRoleList;
        }

        [HttpPost("group/user")]
        public async Task<ActionResult<int>> AssignToGroup(string groupCode)
        {
            Group group;
            try
            {
                group = await _dbContext.Groups.Where(g => g.Code == groupCode).FirstAsync();
            }
            catch (Exception ex)
            {
                if (ex is ArgumentNullException || ex is InvalidOperationException)
                    return BadRequest();
                else 
                    throw;
            }

            var newGroupUser = new GroupUser() {
                UserId = CallerId(),
                GroupId = group.Id,
                Role = Enums.Role.DefaultUser
            };

            await _dbContext.GroupUsers.AddAsync(newGroupUser);
            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                if (ex is DbUpdateException)
                    // 400 - User is already assigned to the group
                    return BadRequest();
                else if(ex is DbUpdateConcurrencyException)
                    // 500 - Internal server error, informs client that error occured, but not due to incorrect request
                    return StatusCode(500);
                else    
                    throw;
            }

            return group.Id;
        }

        private StatusCodeResult NotAuthorized()
        {
            return StatusCode(403);
        }

        private static string GenerateCode()
        {
            return Guid.NewGuid().ToString();
        }

        private string CallerId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier).Value;
        }
    } 

}