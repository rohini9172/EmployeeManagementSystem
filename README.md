# Employee Management System (EMS)

A full-stack Employee Management System built with **ASP.NET Core Web API (.NET 9)**, **React + Vite**, **SQL Server**, and **Redis**. 

---

## Tech Stack

| Layer      | Technology                                      |
|------------|-------------------------------------------------|
| Frontend   | React 18, Vite, React Router, Axios             |
| Backend    | ASP.NET Core Web API (.NET 9), C#               |
| Database   | SQL Server, Entity Framework Core 9             |
| Cache      | Redis (StackExchange.Redis)                     |
| Auth       | JWT Bearer Tokens, BCrypt password hashing      |
| API Docs   | Swagger / Swashbuckle                           |

---

## Project Structure

```
EMS_RedisProject/
├── EMS.API/                        ← ASP.NET Core Web API
│   ├── Controllers/
│   │   ├── AuthController.cs       ← Login, Register, Me, Change Password
│   │   ├── EmployeesController.cs  ← CRUD + Search + View recording
│   │   └── ActivityController.cs   ← Recent, Popular, Summary, Stats
│   ├── Services/
│   │   ├── AuthService.cs          ← JWT generation, BCrypt hashing
│   │   ├── EmployeeService.cs      ← Business logic + Redis cache-aside
│   │   ├── ActivityService.cs      ← All Redis data structure operations
│   │   ├── RedisService.cs         ← Reusable Redis wrapper
│   │   └── CacheKeys.cs            ← All Redis key patterns
│   ├── Repositories/
│   │   ├── EmployeeRepository.cs   ← SQL Server employee queries
│   │   └── UserRepository.cs       ← SQL Server user queries
│   ├── Models/
│   │   ├── User.cs                 ← Users table entity
│   │   └── Employee.cs             ← Employees table entity
│   ├── DTOs/
│   │   ├── EmployeeDTOs.cs         ← Request/Response shapes
│   │   ├── ActivityDTOs.cs         ← Activity response shapes
│   │   └── ServiceResult.cs        ← Success/failure wrapper
│   ├── Data/
│   │   ├── AppDbContext.cs          ← EF Core DbContext
│   │   └── DbSeeder.cs             ← Seeds admin + sample employees
│   ├── Middleware/
│   │   └── ExceptionMiddleware.cs  ← Global error handling
│   └── Program.cs                  ← DI, JWT, Swagger, CORS, Redis setup
│
└── ems-frontend/                   ← React + Vite
    └── src/
        ├── pages/
        │   ├── LoginPage.jsx
        │   ├── RegisterPage.jsx
        │   ├── AdminDashboard.jsx
        │   ├── EmployeeDashboard.jsx
        │   ├── EmployeeListPage.jsx
        │   ├── EmployeeDetailPage.jsx
        │   ├── AddEmployeePage.jsx
        │   ├── EditEmployeePage.jsx
        │   ├── MyProfilePage.jsx
        │   ├── RecentlyViewedPage.jsx
        │   ├── ChangePasswordPage.jsx
        │   └── ActivityPage.jsx
        ├── components/
        │   ├── Navbar.jsx
        │   ├── ProtectedRoute.jsx
        │   └── Toast.jsx
        ├── context/
        │   └── AuthContext.jsx      ← Global auth state + token verification
        ├── hooks/
        │   └── useApi.js            ← Reusable loading/error hook
        └── services/
            ├── api.js               ← Axios instance + JWT interceptor
            ├── authService.js
            ├── employeeService.js
            └── activityService.js
```

---

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [SQL Server](https://www.microsoft.com/en-us/sql-server) (Express or Developer edition)
- [Redis](https://redis.io/download) running locally on port `6379`
- [Node.js](https://nodejs.org/) v18+

### Verify Redis is running
```bash
redis-cli ping
# Expected: PONG
```

---

## Getting Started

### 1. Clone the repository
```bash
git clone <your-repo-url>
cd EMS_RedisProject
```

### 2. Configure the API

Open `EMS.API/appsettings.json` and update:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=EMS_RedisDB;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  }
}
```

Replace `YOUR_SERVER` with your SQL Server instance name (e.g. `localhost\SQLEXPRESS`).

### 3. Run the API

```bash
cd EMS.API
dotnet run
```

- EF Core migrations run automatically on startup
- Seed data is inserted automatically (admin + 3 sample employees)
- Swagger UI: `http://localhost:5013`

### 4. Run the Frontend

```bash
cd ems-frontend
npm install
npm run dev
```

- React app: `http://localhost:5173`

---

## Default Login Credentials

| Role     | Email             | Password      |
|----------|-------------------|---------------|
| Admin    | admin@ems.com     | Admin@123     |
| Employee | alice@ems.com     | Employee@123  |
| Employee | bob@ems.com       | Employee@123  |
| Employee | carol@ems.com     | Employee@123  |

---

## API Endpoints

### Authentication
| Method | Endpoint                    | Auth     | Description                  |
|--------|-----------------------------|----------|------------------------------|
| POST   | /api/auth/register          | Public   | Register (Employee role only)|
| POST   | /api/auth/login             | Public   | Login, returns JWT           |
| GET    | /api/auth/me                | Any      | Verify token, get user info  |
| POST   | /api/auth/change-password   | Any      | Change own password          |

### Employees
| Method | Endpoint                        | Auth     | Description                        |
|--------|---------------------------------|----------|------------------------------------|
| GET    | /api/employees                  | Any      | List all (Redis cache-aside)        |
| GET    | /api/employees/{id}             | Any      | Get by ID (Redis cache + view track)|
| GET    | /api/employees/me               | Any      | Get own profile                    |
| GET    | /api/employees/search?term=     | Any      | Search (rate limited in Phase 7)   |
| POST   | /api/employees                  | Admin    | Create employee                    |
| PUT    | /api/employees/{id}             | Admin/Self | Update employee                  |
| DELETE | /api/employees/{id}             | Admin    | Delete employee                    |

### Activity
| Method | Endpoint                            | Auth     | Description                      |
|--------|-------------------------------------|----------|----------------------------------|
| GET    | /api/activity/recent                | Any      | Recently viewed (Redis List)     |
| GET    | /api/activity/popular               | Any      | Most viewed (Redis Sorted Set)   |
| GET    | /api/activity/summary               | Admin    | Full stats (Redis Set + SortedSet)|
| GET    | /api/activity/employee/{id}/stats   | Admin    | Per-employee stats (Redis Hash)  |

---

## Redis Features Implemented

| Feature              | Redis Structure | Key Pattern                        | Description                              |
|----------------------|-----------------|------------------------------------|------------------------------------------|
| Employee list cache  | String          | `employees:all`                    | Cache-aside, 5 min TTL                   |
| Individual cache     | String          | `employee:{id}`                    | Cache-aside, 5 min TTL                   |
| View count           | Hash            | `activity:employee:{id}`           | `viewCount` + `lastViewed` fields        |
| Recently viewed      | List            | `activity:recent:{userId}`         | Last 5 per user, no duplicates           |
| Unique views         | Set             | `activity:unique-views`            | Unique employee IDs ever viewed          |
| Popularity ranking   | Sorted Set      | `activity:popular`                 | Ranked by view count score               |
| Rate limiting        | String + INCR   | `ratelimit:search:{ip}`            | 10 requests / 60 seconds (Phase 7)       |
| Pub/Sub              | Pub/Sub         | `employee-updated` channel         | Notify on employee update (Phase 7)      |

---

## Redis Key Inspection (redis-cli)

```bash
# See all keys
KEYS *

# Employee list cache
GET employees:all
TTL employees:all

# Individual employee cache
GET employee:1
TTL employee:1

# Activity hash (view count + last viewed)
HGETALL activity:employee:1

# Recently viewed list for user 1
LRANGE activity:recent:1 0 -1

# Unique views set
SMEMBERS activity:unique-views
SCARD activity:unique-views

# Popularity sorted set (highest score first)
ZRANGE activity:popular 0 -1 WITHSCORES REV
```

---

## User Roles & Permissions

| Feature                    | Admin | Employee |
|----------------------------|-------|----------|
| View all employees         | ✅    | ✅       |
| View employee detail       | ✅    | ✅       |
| Search employees           | ✅    | ✅       |
| Add employee               | ✅    | ❌       |
| Edit any employee          | ✅    | ❌       |
| Edit own profile           | ✅    | ✅       |
| Delete employee            | ✅    | ❌       |
| View activity summary      | ✅    | ❌       |
| View recently viewed       | ✅    | ✅ (own) |
| View popular employees     | ✅    | ✅       |

---

## Architecture

```
React (Vite)
    │  Axios + JWT header
    ▼
ASP.NET Core Web API
    │
    ├── Controllers  (HTTP in/out, role checks)
    ├── Services     (business logic + Redis)
    ├── Repositories (SQL Server via EF Core)
    └── Middleware   (global exception handler)
         │
         ├── SQL Server (permanent data)
         └── Redis      (cache + activity tracking)
```

### Cache-Aside Flow
```
GET /api/employees
    │
    ├─ Check Redis (employees:all)
    │       │
    │   HIT ┤ return cached JSON  ← no SQL query
    │       │
    │  MISS ┤ query SQL Server
    │       │ store in Redis (TTL 5 min)
    │       └ return data
```

### Cache Invalidation
On Create / Update / Delete:
1. Write to SQL Server first
2. Delete affected Redis keys (`employees:all`, `employee:{id}`)
3. Next read will be a cache miss → refreshes from SQL Server
4. If Redis delete fails → log warning, do NOT fail the request (TTL will expire stale data)

---

## Development Notes

- JWT tokens are stored in `localStorage` for development simplicity
- In production, use **HttpOnly cookies** to prevent XSS token theft
- Redis `AbortOnConnectFail = false` — app starts even if Redis is unavailable
- All Redis errors are caught and logged — they never crash the app
- SQL Server is always the source of truth — Redis is a performance layer

---

## Phases Completed

| Phase | Description                                          | Status |
|-------|------------------------------------------------------|--------|
| 1     | Project setup, folder structure, architecture        | ✅ Done |
| 2     | SQL Server, EF Core, entities, CRUD APIs             | ✅ Done |
| 3     | JWT authentication, roles, authorization             | ✅ Done |
| 4     | React frontend, API integration, UI polish           | ✅ Done |
| 5     | Redis setup, cache-aside, TTL, invalidation          | ✅ Done |
| 6     | Hash, List, Set, Sorted Set, atomic counters         | ✅ Done |

