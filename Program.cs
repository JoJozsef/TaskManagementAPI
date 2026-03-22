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
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});
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
builder.Services.AddScoped<CommentService>();

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

// Seed data (dev. only)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<TaskDbContext>();

    if (!context.Users.Any())
    {
        // Hash jelszó
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("Test1234!");

        // User létrehozása
        var user = new User
        {
            Email = "test@example.com",
            PasswordHash = passwordHash,
            Name = "Test User",
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        context.SaveChanges();
    }
}

// Endpoints
// register
app.MapPost("/auth/register", async (RegisterRequest request, AuthService service) => 
{
    var user = await service.RegisterAsync(request);
    if (user == null) return Results.Conflict(new { error = "Email already exists" });
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
    if (createdTask == null) return Results.Forbid();

    return Results.Created($"/tasks/{createdTask.Id}", createdTask);
}).RequireAuthorization();

// Get projects task
app.MapGet("/projects/{projectId}/tasks", async (int projectId, TaskService service, HttpContext httpContext) => 
{
    // User ID from JWT token
    var userId = httpContext.GetUserId();
    if (userId == null) return Results.Unauthorized();

    var tasks = await service.GetAllByProjectIdAsync(projectId, userId.Value);
    if (tasks == null) return Results.Forbid();

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

// Update Status
app.MapPatch("/tasks/{id}/status", async (int id, UpdateTaskStatusRequest request, TaskService service, HttpContext httpContext) => 
{
    // User ID from JWT token
    var userId = httpContext.GetUserId();
    if (userId == null) return Results.Unauthorized();

    var success = await service.UpdateStatusAsync(id, request.Status, userId.Value);
    return success ? Results.NoContent() : Results.NotFound();
}).RequireAuthorization();

// CommentService endpoints
app.MapPost("/tasks/{taskId}/comments", async (int taskId, Comment comment, CommentService service, HttpContext httpContext) => 
{
    // User ID from JWT token
    var userId = httpContext.GetUserId();
    if (userId == null) return Results.Unauthorized();

    comment.TaskId = taskId;

    var createdComment = await service.CreateAsync(comment, userId.Value);
    if (createdComment == null) return Results.Forbid();

    return Results.Created($"/comments/{createdComment.Id}", createdComment);
}).RequireAuthorization();

app.MapGet("/tasks/{taskId}/comments", async (int taskId, CommentService service, HttpContext httpContext) => 
{
    // User ID from JWT token
    var userId = httpContext.GetUserId();
    if (userId == null) return Results.Unauthorized();

    var comments = await service.GetAllByTaskIdAsync(taskId, userId.Value);
    if (comments == null) return Results.Forbid();

    return Results.Ok(comments);
}).RequireAuthorization();

app.MapDelete("/comments/{id}", async (int id, CommentService service, HttpContext httpContext) =>
{
    // User ID from JWT token
    var userId = httpContext.GetUserId();
    if (userId == null) return Results.Unauthorized();

    var success = await service.DeleteAsync(id, userId.Value);
    return success ? Results.NoContent() : Results.NotFound();
})
.RequireAuthorization();

app.Run();
public partial class Program { }