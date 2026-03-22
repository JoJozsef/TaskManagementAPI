using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using TaskManagementAPI.Data;
using TaskManagementAPI.Models;

namespace TaskManagementAPI.Services
{
    public class CommentService
    {
        private readonly TaskDbContext _dbContext;
        public CommentService(TaskDbContext dbContext) 
        {
            _dbContext = dbContext;
        }

        public async Task<Comment> CreateAsync (Comment comment, int userId) 
        {
            // Validation + Authorization
            var project = await ValidateTaskAuthorizationAsync(comment.TaskId, userId);
            if (project == null) return null;

            // Comment creation
            comment.CreatedAt = DateTime.UtcNow;
            comment.UserId = userId;
            _dbContext.Comments.Add(comment);
            await _dbContext.SaveChangesAsync();
            return comment;
        }
        public async Task<List<Comment>> GetAllByTaskIdAsync(int taskId, int userId) 
        {
            // Validation + Authorization
            var project = await ValidateTaskAuthorizationAsync(taskId, userId);
            if (project == null) return null;

            // Get comments
            return await _dbContext.Comments.Where(c => c.TaskId == taskId).ToListAsync();
        }
        public async Task<bool> DeleteAsync(int id, int userId) 
        {
            // Comment search
            var comment = await _dbContext.Comments.FindAsync(id);
            if (comment == null) return false;

            // Check if author
            bool isAuthor = comment.UserId == userId;

            // Check if project owner
            var project = await ValidateTaskAuthorizationAsync (comment.TaskId, userId);
            bool isProjectOwner = project != null;

            // Authorization: Either author or project owner
            if (!isAuthor && !isProjectOwner)
            {
                return false;
            }

            // Delete
            _dbContext.Comments.Remove(comment);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        private async Task<Project?> ValidateTaskAuthorizationAsync(int taskId, int userId)
        {
            // Task search
            var task = await _dbContext.Tasks.FindAsync(taskId);
            if (task == null) return null;

            // Project search
            var project = await _dbContext.Projects.FindAsync(task.ProjectId);
            if (project == null) return null;

            // Authorization check
            if (project.OwnerId != userId) return null;

            return project;
        }
    }
    }
