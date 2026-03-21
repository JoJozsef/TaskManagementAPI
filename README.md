# Task Management API

Task and project management REST API with JWT authentication and Kanban board functionality.

## Features

- ✅ JWT Authentication (Register, Login)
- ✅ Project management (CRUD)
- ✅ Task management (CRUD)
- ✅ Kanban board (Status: To Do, In Progress, Done)
- ✅ Priority levels (Low, Medium, High)
- ✅ Authorization (only project owners can modify)
- ✅ Due dates and task assignments

## Tech Stack

- ASP.NET Core 8.0
- Entity Framework Core
- SQLite
- JWT Authentication (BCrypt password hashing)
- Minimal API architecture
- Swagger/OpenAPI documentation

## Getting Started

### Prerequisites

- .NET 8 SDK
- Visual Studio 2022 or VS Code

### Installation

1. Clone the repository
```bash
git clone https://github.com/JoJozsef/TaskManagementAPI.git
cd TaskManagementAPI
```

2. Run database migrations
```bash
dotnet ef database update
```

3. Run the application
```bash
dotnet run
```

4. Open Swagger UI
```
https://localhost:7204/swagger
```

(Check console output for actual port number)

## API Endpoints

### Authentication
- `POST /auth/register` - Register new user
- `POST /auth/login` - Login and get JWT token

### Projects
- `POST /projects` - Create project
- `GET /projects` - Get user's projects
- `GET /projects/{id}` - Get project by ID
- `PUT /projects/{id}` - Update project
- `DELETE /projects/{id}` - Delete project

### Tasks
- `POST /projects/{projectId}/tasks` - Create task in project
- `GET /projects/{projectId}/tasks` - Get all tasks in project
- `GET /tasks/{id}` - Get task by ID
- `PUT /tasks/{id}` - Update task
- `DELETE /tasks/{id}` - Delete task

## Project Structure
```
TaskManagementAPI/
├── Data/
│   ├── DesignTimeDbContextFactory.cs
│   └── TaskDbContext.cs
├── Extensions/
│   └── HttpContextExtensions.cs
├── Models/
│   ├── DTOs/
│   │   ├── AuthResponse.cs
│   │   ├── LoginRequest.cs
│   │   └── RegisterRequest.cs
│   ├── Project.cs
│   ├── ProjectTask.cs
│   └── User.cs
├── Services/
│   ├── AuthService.cs
│   ├── ProjectService.cs
│   └── TaskService.cs
└── Program.cs
```

## Usage Example

1. Register a new user:
```json
POST /auth/register
{
  "email": "user@example.com",
  "password": "Password123!",
  "name": "John Doe"
}
```

2. Login to get JWT token:
```json
POST /auth/login
{
  "email": "user@example.com",
  "password": "Password123!"
}
```

3. Use the token in Swagger (Authorize button) or add header:
```
Authorization: Bearer <your-jwt-token>
```

4. Create a project:
```json
POST /projects
{
  "title": "My Project",
  "description": "Project description"
}
```

5. Create a task:
```json
POST /projects/1/tasks
{
  "title": "Complete feature",
  "description": "Implement new feature",
  "priority": 2,
  "status": 0,
  "dueDate": "2025-03-30T10:00:00"
}
```

## Author

József Boli - [GitHub](https://github.com/JoJozsef)

## License

This project is for educational purposes.