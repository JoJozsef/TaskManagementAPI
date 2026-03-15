using Microsoft.EntityFrameworkCore;
using System.Net.Security;
using TaskManagementAPI.Data;
using TaskManagementAPI.Models.DTOs;
using TaskManagementAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<TaskDbContext>(options => options.UseSqlite("Data Source=tasks.db"));
builder.Services.AddScoped<AuthService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

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

app.Run();
public partial class Program { }