using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http;
using System.Net.Security;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using TaskManagementAPI.Data;
using TaskManagementAPI.Models;
using TaskManagementAPI.Models.DTOs;
using TaskManagementAPI.Services;
using TaskManagementAPI.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<TaskDbContext>(options => options.UseSqlite("Data Source=tasks.db"));
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ProjectService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddScoped<TaskService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Endpoints
// register
app.MapPost("/auth/register", async (RegisterRequest request, AuthService service) => 
{
    var user = await service.RegisterAsync(request);
    return Results.Created($"/users/{user.Id}", new { 
        user.Id,
        user.Email,
        user.Name,        
    });
});

// login 
app.MapPost("/auth/login", async (LoginRequest request, AuthService service) => 
{
    var authResponse = await service.LoginAsync(request);
    if(authResponse == null) return Results.Unauthorized();
    return Results.Ok(authResponse);
});

// Project post (new)
app.MapPost("/projects", async (Project project, ProjectService service, HttpContext httpContext) => 
{
    // User ID from JWT token
    var userId = httpContext.GetUserId();
    if (userId == null) return Results.Unauthorized();
    

    // OwnerId settings
    project.OwnerId = userId.Value;

    // Service call
    var createdProject = await service.CreateAsync(project);

    // Answer
    return Results.Created($"/projects/{createdProject.Id}", createdProject);
})
    .RequireAuthorization();

// Project get (own)
app.MapGet("/projects", async (ProjectService service, HttpContext httpContext) =>
{
    // User ID from JWT token
    var userId = httpContext.GetUserId();
    if (userId == null) return Results.Unauthorized();

    var allProject = await service.GetAllByUserIdAsync(userId.Value);
    return Results.Ok(allProject);

}).RequireAuthorization();

// Project get (one)
app.MapGet("/projects/{id}", async (int id, ProjectService service, HttpContext httpContext) =>
{
    // User ID from JWT token
    var userId = httpContext.GetUserId();
    if (userId == null) return Results.Unauthorized();

    var project = await service.GetByIdAsync(id);
    if(project == null) return Results.NotFound();
    if (project.OwnerId != userId) return Results.Forbid(); 
    return Results.Ok(project);
    
}).RequireAuthorization();

// Project put (mod)
app.MapPut("/projects/{id}", async (int id, Project updatedProject, ProjectService service, HttpContext httpContext) =>
{
    // User ID from JWT token
    var userId = httpContext.GetUserId();
    if (userId == null) return Results.Unauthorized();

    var success = await service.UpdateAsync(id, updatedProject, userId.Value);
    return success ? Results.NoContent() : Results.NotFound();
}).RequireAuthorization();

// Project delete
app.MapDelete("/projects/{id}", async (int id, ProjectService service, HttpContext httpContext) => 
{
    // User ID from JWT token
    var userId = httpContext.GetUserId();
    if (userId == null) return Results.Unauthorized();

    var success = await service.DeleteAsync(id, userId.Value);
    return success ? Results.NoContent(): Results.NotFound();
}).RequireAuthorization();

// Task endpoints
// Post
app.MapPost("/projects/{projectId}/tasks", async (int projectId, ProjectTask task, TaskService service, HttpContext httpContext) => 
{
    // User ID from JWT token
    var userId = httpContext.GetUserId();
    if (userId == null) return Results.Unauthorized();

    task.ProjectId = projectId;

    var createdTask = await service.CreateAsync(task, userId.Value);

    return Results.Created($"/tasks/{createdTask.Id}", createdTask);
}).RequireAuthorization();

// Get projects task
app.MapGet("/projects/{projectId}/tasks", async (int projectId, TaskService service, HttpContext httpContext) => 
{
    // User ID from JWT token
    var userId = httpContext.GetUserId();
    if (userId == null) return Results.Unauthorized();

    var tasks = await service.GetAllByProjectIdAsync(projectId, userId.Value);

    return Results.Ok(tasks);
}).RequireAuthorization();

// Get a task
app.MapGet("/tasks/{id}", async (int id, TaskService service, HttpContext httpContext) => 
{
    // User ID from JWT token
    var userId = httpContext.GetUserId();
    if (userId == null) return Results.Unauthorized();

    var task = await service.GetByIdAsync(id, userId.Value);
    if (task == null) return Results.NotFound();

    return Results.Ok(task);

}).RequireAuthorization();

// Put task
app.MapPut("/tasks/{id}", async (int id, ProjectTask updatedTask ,TaskService service, HttpContext httpContext) => 
{
    // User ID from JWT token
    var userId = httpContext.GetUserId();
    if (userId == null) return Results.Unauthorized();

    var success = await service.UpdateAsync(id, updatedTask, userId.Value);
    return success ? Results.NoContent() : Results.NotFound();
    
}).RequireAuthorization();

// Delete task
app.MapDelete("/tasks/{id}", async (int id, TaskService service, HttpContext httpContext) => 
{
    // User ID from JWT token
    var userId = httpContext.GetUserId();
    if (userId == null) return Results.Unauthorized();

    var success = await service.DeleteAsync(id, userId.Value);
    return success ? Results.NoContent() : Results.NotFound();

}).RequireAuthorization();


app.Run();
public partial class Program { }