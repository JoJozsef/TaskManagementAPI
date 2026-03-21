using TaskManagementAPI.Data;
using TaskManagementAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace TaskManagementAPI.Services
{
    public class TaskService
    {
        private readonly TaskDbContext _dbContext;

        public TaskService(TaskDbContext dbContext) {
            _dbContext = dbContext;
        }

        public async Task<ProjectTask> CreateAsync(ProjectTask task, int userId)
        {
            // Project search
            var project = await _dbContext.Projects.FindAsync(task.ProjectId);
            if (project == null) throw new Exception("Project not found");

            // Auth check
            if (project.OwnerId != userId) throw new Exception("Unauthorized");

            // Task settings
            task.CreatedAt = DateTime.UtcNow;

            //Default status
            task.Status = TaskManagementAPI.Models.TaskStatus.ToDo;
            // Save
            _dbContext.Tasks.Add(task);
            await _dbContext.SaveChangesAsync();

            return task;
        }

        public async Task<List<ProjectTask>> GetAllByProjectIdAsync(int projectId, int userId)
        {
            // Project search
            var project = await _dbContext.Projects.FindAsync(projectId);
            if (project == null) throw new Exception("Project not found");

            // Auth check
            if (project.OwnerId != userId) throw new Exception("Unauthorized");

            // Tasks
            return await _dbContext.Tasks
                .Where(t => t.ProjectId == projectId)
                .ToListAsync();
        } 

        public async Task<ProjectTask?> GetByIdAsync(int id, int userId)
        {
            // Task search
            var task = await _dbContext.Tasks
                .FindAsync(id);
            if (task == null) return null;

            // Project search
            var project = await _dbContext.Projects.FindAsync(task.ProjectId);
            if (project == null) return null;

            // Auth chech
            if (project.OwnerId != userId) return null;

            return task;

        }

        public async Task<bool> UpdateAsync(int id, ProjectTask updatedTask, int userId)
        {
            // Task search
            var task = await GetByIdAsync(id, userId);
            if (task == null) return false;

            // Update
            task.Title = updatedTask.Title;
            task.Description = updatedTask.Description;
            task.Status = updatedTask.Status;
            task.Priority = updatedTask.Priority;
            task.AssignedToId = updatedTask.AssignedToId;
            task.DueDate = updatedTask.DueDate;

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id, int userId)
        {
            // Task search
            var task = await GetByIdAsync(id, userId);
            if (task == null) return false;

            _dbContext.Tasks.Remove(task);
            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}
