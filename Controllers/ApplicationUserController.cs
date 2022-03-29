using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using System;

using SWZZ_Backend.Data;
using SWZZ_Backend.DTO;
using SWZZ_Backend.Models;

namespace SWZZ_Backend.Controllers
{
    [Authorize]
    [ApiController]
    public class ApplicationUserController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public ApplicationUserController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _dbContext = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterForm register)
        {
            // Check whether user is logged, only unouthorized users can register
            if (_signInManager.IsSignedIn(User)){
                return BadRequest();
            }
            // Check if any field is empty:
            if(String.IsNullOrWhiteSpace(register.Email) || String.IsNullOrWhiteSpace(register.Name) 
                || String.IsNullOrWhiteSpace(register.Surname) || String.IsNullOrWhiteSpace(register.Password)){
                return BadRequest();
            }
            string normalizedEmail = register.Email.Normalize();
            ApplicationUser alreadyUser;
            // Check if email is already in use:
            try{
                alreadyUser = await _dbContext.ApplicationUsers.Where(u=>u.NormalizedEmail == normalizedEmail).FirstAsync();
            }catch{
                alreadyUser=null;
            }
            if(alreadyUser==null){
                var user = new ApplicationUser { UserName = register.Email, Email = register.Email, Name = register.Name, Surname = register.Surname };
                var registerResult = await _userManager.CreateAsync(user, register.Password);
                if (registerResult.Succeeded)
                {
                    var loginResult = await _signInManager.PasswordSignInAsync(register.Email, register.Password, false, lockoutOnFailure: false);
                    if (loginResult.Succeeded)
                        return Ok();
                    else
                        return ValidationProblem();
                }
            }
            return BadRequest();
        }
        
        [HttpGet("login")]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return NoContent();
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginForm login)
        {
            var result = await _signInManager.PasswordSignInAsync(login.Email, login.Password, login.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
                return Ok();
            else 
                return ValidationProblem();
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok();
        }

        [HttpGet("user")]
        public async Task<ActionResult<UserDTO>> GetUser()
        {
            Func<Task<List<ApplicationUser>>> GetApplicationUser = async () => {return await _dbContext.ApplicationUsers.Where(u => u.Id == CallerId()).ToListAsync();};
            var user = await GetApplicationUser();
            return  new UserDTO(user.First());
        }

        [HttpGet("user/tasks")]
        public async Task<ActionResult<IEnumerable<TaskAndPermissionsDTO>>> GetAssignedTasks()
        {
            Func<Task<List<TaskItem>>> GetAssignedTaskItems = async () => {return await _dbContext.TaskItems.Where(t => t.CommissioneeName == CallerId()).ToListAsync();};
            var taskItems = await GetAssignedTaskItems();
            var taskAndPermissionsDTOList = new List<TaskAndPermissionsDTO>();
            //Gets NameIdentifier of current User
            var userIdentifier = this.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            foreach (var task in taskItems)
            {
                //Gets user Role in current group:
                GroupUser groupRole = await _dbContext.GroupUsers.FindAsync(userIdentifier, task.GroupId);
                //Set permission level accordingly to role in the group.
                TaskPermissions permission = new TaskPermissions(groupRole.Role);
                if(task.CommissionerName == userIdentifier)
                    permission = new TaskPermissions(Enums.Role.Administrator);
                
                var taskAndPermissionsDTO = new TaskAndPermissionsDTO(
                    new TaskItemDTO(task), 
                    permission
                    );
                taskAndPermissionsDTOList.Add(taskAndPermissionsDTO);
            }
            return taskAndPermissionsDTOList;
        }

        [HttpGet("user/groups")]
        public async Task<ActionResult<IEnumerable<GroupAndPermissionsDTO>>> GetGroups()
        {
            // Get list of all groups and user roles in them
            var groupAndRoleList = await _dbContext.GroupUsers
                .Where(gUser => gUser.UserId == CallerId())
                .Join(_dbContext.Groups,
                    gUser => gUser.GroupId,
                    g => g.Id,
                    (gUser, g) => new {
                        GroupId = g.Id,
                        GroupName = g.Name,
                        GroupIcon = g.Icon,
                        GroupDescription = g.Description,
                        UserRole = gUser.Role
                }).ToListAsync();

            // Repack above list separating group info and user role
            // User role is necessary for getting user permissions
            var groupAndPermissionsDTOList = new List<GroupAndPermissionsDTO>();
            foreach(var groupAndRole in groupAndRoleList)
            {
                groupAndPermissionsDTOList.Add(new GroupAndPermissionsDTO(
                    new GroupDTO() 
                    {
                        GroupId = groupAndRole.GroupId, 
                        Name = groupAndRole.GroupName, 
                        Icon = groupAndRole.GroupIcon,
                        Description = groupAndRole.GroupDescription
                    },
                    new GroupPermissions(groupAndRole.UserRole)
                ));
            }

            return groupAndPermissionsDTOList;
        }

        [HttpPost("user/password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordForm form)
        {
            ApplicationUser callingUser = await _userManager.FindByIdAsync(CallerId());

            bool isOldPasswordCorrect = await _userManager.CheckPasswordAsync(callingUser, form.OldPassword);
            if (!isOldPasswordCorrect)
                return BadRequest();

            bool isNewPasswordValid = false;
            foreach (var validator in  _userManager.PasswordValidators.ToList())
            {
                var validationResult = await validator.ValidateAsync(_userManager, callingUser, form.NewPassword);
                if (validationResult.Succeeded)
                {
                    isNewPasswordValid = true;
                    break;
                }
            }

            if (!isNewPasswordValid)
                return BadRequest();

            string passwordResetToken = await _userManager.GeneratePasswordResetTokenAsync(callingUser);
            var result = await _userManager.ResetPasswordAsync(callingUser, passwordResetToken, form.NewPassword);
            if (result.Succeeded)
                return Ok();
            else
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status500InternalServerError); // Something went wrong, return server error code
        }

        [HttpDelete("user")]
        public async Task<IActionResult> DeleteUser()
        {
            List<GroupUser> userRoles;
            try
            {
                userRoles = await _dbContext.GroupUsers.Where(u => u.UserId == CallerId()).ToListAsync();
            }
            catch (ArgumentNullException)
            {
                return BadRequest();
            }

            foreach (var userRole in userRoles)
            {
                try
                {
                    Group group = await _dbContext.Groups.Where(g => g.Id == userRole.GroupId).FirstAsync();
                    _dbContext.GroupUsers.Remove(userRole);

                    var unassignedTasks = await _dbContext.TaskItems
                    .Where( t => t.CommissioneeName.Equals(userRole.UserId) && t.GroupId == userRole.GroupId).ToListAsync();
                    foreach (var taskItem in unassignedTasks){ //Unassign user from tasks
                        taskItem.CommissioneeName=null;
                    }

                    List<GroupUser> restOfGroupUsers = await _dbContext.GroupUsers.Where(g => g.GroupId == userRole.GroupId && g.UserId != CallerId()).ToListAsync();

                    if (restOfGroupUsers.Count == 0)
                    {
                        _dbContext.Groups.Remove(group);
                        _dbContext.TaskItems.RemoveRange(_dbContext.TaskItems.Where(t => t.GroupId == group.Id));
                    }
                    else if (!restOfGroupUsers.Exists(g => g.Role == Enums.Role.Administrator))
                    {
                        foreach(var groupUser in restOfGroupUsers)
                            groupUser.Role = Enums.Role.Administrator;
                    }
                }
                catch (Exception ex)
                {
                    if (ex is ArgumentNullException || ex is InvalidOperationException)
                        return StatusCode(500); // 500 - Internal server error- Informs client that error occured, but not due to incorrect request
                    else
                        throw;
                }
                
            }

            var thisUser = await _dbContext.Users.FindAsync(CallerId());
            if (thisUser == null)
                return StatusCode(500);
            _dbContext.Users.Remove(thisUser);

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

            await _signInManager.SignOutAsync();
            return Ok();
        }

        private string CallerId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier).Value;
        }
    }    
}