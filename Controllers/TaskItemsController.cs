using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWZZ_Backend.Models;
using SWZZ_Backend.Data;
using SWZZ_Backend.Enums;
using SWZZ_Backend.DTO;
using SWZZ_Backend.Authorization;

namespace SWZZ_Backend.Controllers
{
    using static AuthorizationRole;
    [ApiController]
    public class TaskItemsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ITaskAuthorizationHandler _authorizationHandler;
        public TaskItemsController(ApplicationDbContext context, ITaskAuthorizationHandler authorizationHandler)
        {
            _context = context;
            _authorizationHandler = authorizationHandler;
        }

        [HttpPost("task")]
        public async Task<ActionResult<TaskAndPermissionsDTO>> PostTaskItem(TaskItemDTO taskItemDTO)
        {
            //Try converting TaskItemDTO to TaskItem
            TaskItem taskItem = new TaskItem();
            GroupUser groupUser;
            try {
                taskItemDTO.CopyToTaskItem(taskItem);
                groupUser = await _context.GroupUsers.Where(u => u.UserId == CallerId() && u.GroupId == taskItem.GroupId).FirstAsync();
                
            }
            catch (Exception ex) {
                if (ex is ArgumentException || ex is InvalidOperationException)
                    return BadRequest();
                else
                    throw;
            }

            /*** TaskItem authorization ***/
            if (groupUser.Role != Role.Administrator && groupUser.Role != Role.PrivilegedUser)
                return StatusCode(403);

            var commissioneeAsGroupUser = await _context.GroupUsers.FindAsync(taskItemDTO.CommissioneeId, taskItemDTO.GroupId);
                if (commissioneeAsGroupUser == null)
                    taskItem.CommissioneeName = null;

            //Id is autoincremented when it's value equals 0 when adding to DB
            taskItem.Id = 0;
            //Sets execution time parameter to 1
            taskItem.EstimatedExecutionTime=1;
            //Sets start time to [execution time] before the deadline
            taskItem.StartTime=taskItem.Deadline;
            taskItem.StartTime.AddHours(-taskItem.EstimatedExecutionTime);
            //Sets status to doing (default for creating new task)
            taskItem.Status=TaskItemStatus.ToDo;
            //Gets nameIdentifier of current user (creator of task) and assigns it as creator.
            var currentUserName = this.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            taskItem.CommissionerName = currentUserName;
            taskItem.TaskFailed = false;
            //Adds new Task Item element to collection.
            _context.TaskItems.Add(taskItem);
            //Saves changes asynch.
            await _context.SaveChangesAsync();
            return new TaskAndPermissionsDTO(new TaskItemDTO(taskItem), new TaskPermissions(groupUser.Role));
        }
        
        [HttpGet("task")]
        [TaskAuthorize(AnyMember)]
        public async Task<ActionResult<TaskAndPermissionsDTO>> GetTaskItem(int taskId)
        {
            if (!(await _authorizationHandler.Authorize(User, taskId)))
               return StatusCode(403);

            //gets entity with requested 'id', awaits checks if asynch operation is safe to perform.
            TaskItem taskItem;
            GroupUser groupUser;
            try {
                taskItem = await _context.TaskItems.Where(t => t.Id == taskId).FirstAsync();
                groupUser = await _context.GroupUsers.Where(u => u.UserId == CallerId() && u.GroupId == taskItem.GroupId).FirstAsync();
            }
            catch (Exception ex) {
                if (ex is ArgumentException || ex is InvalidOperationException)
                    return BadRequest();
                else
                    throw;
            }
            //returns desired task item.
            return new TaskAndPermissionsDTO(new TaskItemDTO(taskItem), new TaskPermissions(groupUser.Role));
        }

        //Method PUT updates existing element, changing task attributes like description, title, failure
        [HttpPut("task/attributes")]
        [TaskAuthorize(Administrator, Owner)]
        public async Task<IActionResult> EditTaskAttributes(TaskItemDTO taskItemDTO)
        {
            if (!(await _authorizationHandler.Authorize(User, taskItemDTO.TaskId)))
               return StatusCode(403);

            //Get task from database using given id
            TaskItem taskItem = await _context.TaskItems.FindAsync(taskItemDTO.TaskId);
            if (taskItem == null)
                return BadRequest();

            taskItem.Title = taskItemDTO.Title;
            taskItem.Description = taskItemDTO.Description;
            taskItem.TaskFailed = taskItemDTO.TaskFailed;
            //Changes state of entry to be 'Modified'
            _context.Entry(taskItem).State = EntityState.Modified;
            //Asynchronously save changes to edited elements.
            try {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException) {
                if (!TaskItemExists(taskItem.Id))
                    return NotFound();
                else 
                    throw;
            }
            //Returns empty 204 response (method in ControllerBase )
            return NoContent();
        }

        //Method PUT updates existing element, changing task attributes
        [HttpPut("task/assign")]
        [TaskAuthorize(AnyMember)]
        public async Task<IActionResult> AssignTask(int taskId, string userId)
        {
            if (!(await _authorizationHandler.Authorize(User, taskId)))
               return StatusCode(403);

            TaskItem taskItem;
            bool isUserGroupMember;
            try 
            {
                taskItem = await _context.TaskItems.Where(t => t.Id == taskId).FirstAsync();
                isUserGroupMember = (userId == "0") ? true : await _context.GroupUsers.AnyAsync(u => u.UserId == userId && u.GroupId == taskItem.GroupId);
            }
            catch (Exception ex) {
                if (ex is ArgumentException || ex is InvalidOperationException)
                    return BadRequest();
                else
                    throw;
            }

            if (!isUserGroupMember)
                return BadRequest();

            taskItem.CommissioneeName = (userId == "0") ? null : userId;
            _context.Entry(taskItem).State = EntityState.Modified;
            try {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException) {
                if (!TaskItemExists(taskItem.Id))
                    return NotFound();
                else
                    throw;
            }
            return NoContent();
        }

        [HttpPut("task/status")]
        [TaskAuthorize(AnyMember)]
        public async Task<IActionResult> ChangeTaskStatus(int taskId, string taskItemStatus)
        {
            if (!(await _authorizationHandler.Authorize(User, taskId)))
               return StatusCode(403);

            TaskItem taskItem;
            TaskItemStatus status;
            try 
            {
                taskItem = await _context.TaskItems.Where(t => t.Id == taskId).FirstAsync();
                status = TaskItemDTO.ParseTaskStatus(taskItemStatus);
            }
            catch (Exception ex) {
                if (ex is ArgumentException || ex is InvalidOperationException)
                    return BadRequest();
                else
                    throw;
            }

            taskItem.Status = status;
            _context.Entry(taskItem).State = EntityState.Modified;

            try {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException) {
                if (!TaskItemExists(taskItem.Id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        [HttpDelete("task")]
        [TaskAuthorize(Administrator, Owner)]
        public async Task<IActionResult> DeleteTaskItem(int taskId)
        {
            if (!(await _authorizationHandler.Authorize(User, taskId)))
               return StatusCode(403);

            //Gets item by id
            var taskItem = await _context.TaskItems.FindAsync(taskId);
            if (taskItem == null)
                return NotFound();
            //Removes item form _context 
            _context.TaskItems.Remove(taskItem);
            //Saves changes asynch.
            await _context.SaveChangesAsync();
            //Returns empty 204 response (method in ControllerBase )
            return NoContent();
        }

        //Checks if task with given ID exists.
        private bool TaskItemExists(int id) {
            return _context.TaskItems.Any(e => e.Id == id);
        }

        private string CallerId() {
            return User.FindFirst(ClaimTypes.NameIdentifier).Value;
        }
    }
}
