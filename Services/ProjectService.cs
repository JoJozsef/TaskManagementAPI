using Microsoft.EntityFrameworkCore;
using TaskManagementAPI.Data;
using TaskManagementAPI.Models;

namespace TaskManagementAPI.Services
{
    public class ProjectService
    {
        private readonly TaskDbContext _dbContext;

        public ProjectService(TaskDbContext dbContext) 
        { 
            _dbContext = dbContext;
        }

        public async Task<Project> CreateAsync(Project project) 
        {
            // CrearedAt setting
            project.CreatedAt = DateTime.UtcNow;
            _dbContext.Projects.Add(project);

            // DB save
            await _dbContext.SaveChangesAsync();

            // Project return
            return project;
        }

        public async Task<List<Project>> GetAllByUserIdAsync(int userId)
        {
            return await _dbContext.Projects.Where(p => p.OwnerId == userId).ToListAsync();
        }

        public async Task<Project?> GetByIdAsync(int id)
        {
            return await _dbContext.Projects.FindAsync(id);
        }

        public async Task<bool> UpdateAsync(int id, Project updatedProject, int userId)
        {
            // project.OwnerId == userId
            var project = await GetByIdAsync(id);
            if (project == null) return false;

            if(project.OwnerId != userId) return false;

            project.Title = updatedProject.Title;
            project.Description = updatedProject.Description;

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id, int userId)
        {
            var project = await GetByIdAsync(id);
            if (project == null) return false;

            if (project.OwnerId != userId) return false;

            _dbContext.Projects.Remove(project);
            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}
