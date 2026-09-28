# ResolveIQ — AI-Assisted IT Service & Support Management System

ResolveIQ is an enterprise-inspired IT Service & Support Management application developed using **ASP.NET Core MVC**, **ASP.NET Core Web API**, **C#**, and **Entity Framework Core** with **SQL Server**. It provides role-based ticketing workflows across Employees, Technicians, and Administrators, augmented with a RESTful Web API and AI-driven triage assistance.

---

## 1. Project Overview
ResolveIQ is designed to streamline IT support operations within an organization. It allows employees to quickly log IT incidents, enables technicians to manage and resolve assigned tasks, and empowers system administrators with complete oversight over tickets, user accounts, technicians, and ticket categories. In addition to server-rendered web portals, ResolveIQ exposes a RESTful API with Swagger documentation and incorporates AI assistance for automatic summary generation and intelligent category/priority suggestions.

---

## 2. Problem Statement
In traditional internal helpdesk environments:
- Users struggle to articulate IT problems concisely, leading to vague or overly verbose descriptions.
- Incorrect categorization and misassigned priorities cause delays and SLA violations.
- Support teams lack centralized visibility, leading to unassigned or forgotten tickets.
- Third-party systems lack standardized APIs to programmatically log and monitor tickets.

ResolveIQ resolves these pain points by offering structured role-based workflows, automated status tracking, RESTful integration points, and human-in-the-loop AI assistance.

---

## 3. Features
- **Role-Based Authentication**: Secure cookie authentication separating permissions for Users, Technicians, and Administrators.
- **Full Ticket Lifecycle Management**: Create, assign, track, update status (`Open`, `Assigned`, `In Progress`, `Resolved`, `Closed`), and add resolution notes.
- **Search & Multi-Criteria Filtering**: Filter tickets by status, priority, category, and keyword search.
- **Role-Specific Dashboards**: Real-time metrics and recent ticket summaries tailored for each user role.
- **RESTful Web API**: Full CRUD API endpoints adhering to standard HTTP verbs and status codes.
- **Interactive Swagger Documentation**: Built-in OpenAPI UI for endpoint inspection and live testing.
- **AI-Assisted Triage**:
  - ✨ **AI Summary**: Generates a single-sentence synopsis from verbose descriptions.
  - ✨ **Category Suggestion**: Evaluates title and description to recommend the best matching category.
  - ✨ **Priority Recommendation**: Assesses business impact to recommend `Low`, `Medium`, `High`, or `Critical`.
  - **Human-in-the-Loop**: All AI outputs are suggestions only; users can edit, accept, or ignore them.
- **Graceful Error Handling**: Resilient architecture that falls back smoothly if the AI service or network is unavailable.

---

## 4. User Roles & Permissions

| Role | Responsibilities & Capabilities | Accessible Pages |
|---|---|---|
| **User (Employee)** | Submits support tickets with AI assistance, tracks own tickets, edits/deletes open tickets. | `/User/Dashboard`, `/User/MyTickets`, `/User/CreateTicket`, `/User/Details/{id}`, `/User/EditTicket/{id}` |
| **Technician** | Views tickets specifically assigned to them, updates ticket progress, logs resolution notes upon fixing issues. | `/Technician/Dashboard`, `/Technician/AssignedTickets`, `/Technician/Details/{id}` |
| **Administrator** | Oversees all tickets, assigns technicians, activates/deactivates user and technician accounts, manages categories. | `/Admin/Dashboard`, `/Admin/Tickets`, `/Admin/Users`, `/Admin/Technicians`, `/Admin/Categories` |

---

## 5. Technology Stack
- **Framework**: .NET 10 (ASP.NET Core)
- **Languages**: C#, HTML5, CSS3, JavaScript
- **Web Architectures**: ASP.NET Core MVC + ASP.NET Core Web API
- **Data Access & ORM**: Entity Framework Core 10 (Code-First with Migrations)
- **Database**: Microsoft SQL Server LocalDB (`(localdb)\mssqllocaldb`)
- **API Tooling**: Swagger / OpenAPI (`Swashbuckle.AspNetCore`)
- **AI Integration**: Isolated `AiService` consuming OpenAI-compatible Chat Completion API (`gpt-4o-mini`) via typed `HttpClient`
- **UI Framework**: Bootstrap 5 with responsive layouts

---

## 6. Project Architecture
The project follows a clean, maintainable MVC structure with separated concerns:
```text
ResolveIQ/
│
├── Controllers/                # MVC & Web API Controllers
│   ├── AccountController.cs    # Authentication (Login, Register, Logout)
│   ├── AdminController.cs      # Administrator operations & management
│   ├── HomeController.cs       # Landing page & error handling
│   ├── TechnicianController.cs # Technician operations & status updates
│   ├── TicketsApiController.cs # RESTful Web API with Swagger
│   └── UserController.cs       # Employee portal & AI AJAX actions
│
├── Models/                     # Core Domain Entities
│   ├── Category.cs             # Ticket categories (Hardware, Software, etc.)
│   ├── Technician.cs           # IT technician profiles
│   ├── Ticket.cs               # IT support ticket entity
│   └── User.cs                 # User account entity with role
│
├── DTOs/                       # Data Transfer Objects
│   ├── AiDtos.cs               # AI request & response payloads
│   └── TicketDtos.cs           # API request/response contracts (No cycles)
│
├── ViewModels/                 # Razor View binding models
│   ├── LoginViewModel.cs       # Login form validation
│   └── RegisterViewModel.cs    # Registration form validation
│
├── Services/                   # Business Services & Helpers
│   ├── AiService.cs            # AI integration with validation & timeouts
│   ├── AiServiceException.cs   # Graceful AI exception handling
│   ├── IAiService.cs           # AI service interface
│   └── PasswordHelper.cs       # SHA-256 password hashing
│
├── Data/                       # EF Core Database Context
│   └── ApplicationDbContext.cs # DbSets, entity relations & seed data
│
├── Migrations/                 # EF Core Code-First migrations
│
├── Views/                      # Razor Views organized by Controller
│   ├── Account/                # Login, Register, AccessDenied
│   ├── Admin/                  # Admin Dashboard, Tickets, Users, etc.
│   ├── Home/                   # Welcome page with demo credentials
│   ├── Shared/                 # _Layout.cshtml, _ValidationScriptsPartial
│   ├── Technician/             # Technician Dashboard, Assigned Tickets
│   └── User/                   # User Dashboard, CreateTicket, MyTickets
│
├── wwwroot/                    # Static assets (Bootstrap, CSS, JS)
├── appsettings.json            # Configuration placeholders (No secrets)
└── Program.cs                  # Dependency Injection & middleware pipeline
```

---

## 7. Database Overview
Entity Framework Core Code-First manages the SQL Server database schema with relational integrity:
- **`Users`**: Primary key `Id`, `FullName`, `Email` (unique), `Password` (hashed), `Role`, `IsActive`.
- **`Technicians`**: Primary key `Id`, `FullName`, `Email`, `Phone`, `IsActive`.
- **`Categories`**: Primary key `Id`, `Name` (`Hardware`, `Software`, `Network`, `Account`, `Other`).
- **`Tickets`**: Primary key `Id`, `Title`, `Description`, `Priority`, `Status`, `CreatedDate`, `UpdatedDate`, `ResolutionNote`.
  - Foreign Key: `UserId` $\rightarrow$ `Users.Id` (`OnDelete(DeleteBehavior.Restrict)`)
  - Foreign Key: `CategoryId` $\rightarrow$ `Categories.Id` (`OnDelete(DeleteBehavior.Restrict)`)
  - Foreign Key: `TechnicianId` $\rightarrow$ `Technicians.Id` (Nullable, `OnDelete(DeleteBehavior.SetNull)`)

---

## 8. MVC Flow
```text
User / Browser
      │
      ▼ HTTP Request (e.g. GET /User/CreateTicket or POST /User/CreateTicket)
ASP.NET Core Routing (Endpoint Middleware)
      │
      ▼
UserController
      │
      ├─► Model Binding (maps form inputs / JSON to C# model)
      ├─► Data Annotation Validation (checks [Required], [StringLength])
      ├─► ApplicationDbContext (queries or attaches entities via EF Core)
      │         │
      │         ▼
      │   SQL Server (executes generated SQL commands)
      │
      ▼
Returns ViewResult (Razor view rendered to HTML) or JSON
```

---

## 9. Web API
`TicketsApiController` provides programmatic RESTful access:
- Decorated with `[ApiController]` and `[Route("api/[controller]")]`.
- Employs dedicated DTOs ([DTOs/TicketDtos.cs](file:///d:/New%20folder/ResolveIQ/DTOs/TicketDtos.cs)) to eliminate circular reference serializations.
- Returns standard HTTP status codes:
  - `200 OK` (successful retrieval/update)
  - `201 Created` with `Location` header (ticket created)
  - `204 No Content` (ticket deleted)
  - `400 Bad Request` (model validation failures)
  - `404 Not Found` (nonexistent ticket ID)

---

## 10. AI Features
ResolveIQ features three targeted AI triage capabilities built around human-in-the-loop decision-making:
1. **AI Ticket Summary**: Condenses long descriptions into a clean, 1-sentence summary. Preserves the user's original text unless they explicitly choose to replace it.
2. **Category Suggestion**: Evaluates title and description against the database's valid categories.
3. **Priority Suggestion**: Determines business urgency (`Low`, `Medium`, `High`, `Critical`).

### Output Validation
AI output is never trusted blindly:
- Suggested categories must strictly match one of the database categories.
- Suggested priorities must strictly match `Low`, `Medium`, `High`, or `Critical`.
- In case of unrecognized output or network failure, the application notifies the user and allows manual selection.

---

## 11. Authentication & Authorization
- **Cookie Authentication**: Configured via `builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)`.
- **Role-Based Authorization**: Enforced on controllers and actions using `[Authorize(Roles = "Admin")]`, `[Authorize(Roles = "Technician")]`, and `[Authorize(Roles = "User")]`.
- **URL Tampering Protection**: Even if a user knows the URL `/Admin/Dashboard` or another user's ticket ID `/User/Details/3`, authorization checks verify permissions and reject unauthorized requests with HTTP `403 Forbidden` or redirect to `/Account/AccessDenied`.

---

## 12. How to Run the Project

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/)
- Visual Studio 2022 / VS Code
- SQL Server LocalDB (installed by default with Visual Studio)

### Steps
1. Open terminal in the project directory:
   ```bash
   cd "d:/New folder/ResolveIQ"
   ```
2. Restore dependencies and build the solution:
   ```bash
   dotnet build
   ```
3. Run the application:
   ```bash
   dotnet run --launch-profile http
   ```
4. Open your browser and navigate to:
   - Web App: `http://localhost:5106`
   - Swagger API Docs: `http://localhost:5106/swagger`

---

## 13. Database Setup
The application uses Entity Framework Core Code-First. The database schema and seed data are applied via EF Core migrations:
```bash
dotnet ef database update
```
### Seed Credentials for Quick Testing:
| Role | Email | Password |
|---|---|---|
| **Admin** | `admin@resolveiq.com` | `Admin@123` |
| **Technician** | `technician@resolveiq.com` | `Tech@123` |
| **User** | `user@resolveiq.com` | `User@123` |

---

## 14. AI API Configuration

> **IMPORTANT**: Never hard-code API keys in C# files or commit them to GitHub.

### Using .NET User Secrets (Recommended for Local Development)
```bash
dotnet user-secrets init
dotnet user-secrets set "AiSettings:ApiKey" "your-actual-api-key-here"
```

### Using Environment Variables
```powershell
$env:AiSettings__ApiKey="your-actual-api-key-here"
```

### Fallback Behavior
If an API key is not supplied, `AiService` gracefully catches the missing configuration and informs the user:
> *"AI service is currently unavailable. You can continue creating the ticket manually."*

The application remains completely operational without an API key.

---

## 15. API Endpoints

| Method | Endpoint | Description | Status Code |
|---|---|---|:---:|
| `GET` | `/api/TicketsApi` | Retrieve all tickets | `200 OK` |
| `GET` | `/api/TicketsApi/{id}` | Retrieve ticket by ID | `200 OK` / `404 Not Found` |
| `POST` | `/api/TicketsApi` | Create new ticket | `201 Created` / `400 Bad Request` |
| `PUT` | `/api/TicketsApi/{id}` | Update ticket fields | `200 OK` / `400 Bad Request` / `404 Not Found` |
| `DELETE` | `/api/TicketsApi/{id}` | Delete ticket by ID | `204 No Content` / `404 Not Found` |
| `POST` | `/api/TicketsApi/ai/summary` | Generate AI summary via API | `200 OK` / `400 Bad Request` |
| `POST` | `/api/TicketsApi/ai/suggest-category` | Suggest category via API | `200 OK` / `400 Bad Request` |
| `POST` | `/api/TicketsApi/ai/suggest-priority` | Suggest priority via API | `200 OK` / `400 Bad Request` |

---

## 16. Screenshots Section
*(Screenshots can be added here showing the User Dashboard, Create Ticket with AI suggestions, Admin Ticket Assignment, and Swagger UI.)*

1. **User Dashboard**: Summary metric cards (Total, Open, In Progress, Resolved) and recent ticket history table.
2. **AI-Assisted Ticket Creation**: One-click summary generation, category suggestion badge, and priority recommendation pill.
3. **Admin Dashboard**: Global IT support metrics, technician workload tracking, and category management.
4. **Swagger UI**: Interactive API documentation at `/swagger`.

---

## 17. Future Improvements
- Email/SMS notifications when a ticket is assigned or resolved.
- File attachment upload support (screenshots/logs) stored in blob storage.
- Real-time ticket status updates using ASP.NET Core SignalR.
- SLA escalation timers for tickets in `Open` status beyond 24 hours.
