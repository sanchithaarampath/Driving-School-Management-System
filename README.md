# Driving School Management System (DSMS)

> Final Year Computing Project — BSc (Hons) Software Engineering  
> University of Plymouth | PUSL3190 | 2025–2026

A full-stack web application that digitises and streamlines the core administrative operations of a multi-branch driving school. The system replaces manual, paper-based workflows with a role-aware digital platform covering student enrolment, instalment billing, training scheduling, exam tracking, staff management, and automated receipt delivery.

---

## Table of Contents

- [Overview](#overview)
- [Tech Stack](#tech-stack)
- [System Architecture](#system-architecture)
- [Features](#features)
- [User Roles & Permissions](#user-roles--permissions)
- [Project Structure](#project-structure)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
  - [Backend Setup](#backend-setup)
  - [Frontend Setup](#frontend-setup)
- [Environment Configuration](#environment-configuration)
- [API Overview](#api-overview)
- [Database Schema](#database-schema)
- [Development Workflow](#development-workflow)

---

## Overview

Driving schools in Sri Lanka largely rely on manual processes for student registration, payment collection, and record-keeping. DSMS addresses this by providing a centralised, web-based platform that supports:

- **Multi-branch operations** with strict branch-level data isolation
- **Instalment-based billing** with unique bill numbers, running balance tracking, and automatic receipt generation
- **Dynamic training schedules** driven by the course package's configured training day count
- **Practical test tracking** with pass/fail recording and automatic license number storage upon passing
- **Automated receipt delivery** via email (Gmail SMTP) and WhatsApp (Twilio)
- **Role-based access control** ensuring each user type sees only what they need

---

## Tech Stack

### Backend

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 10 Web API |
| ORM | Entity Framework Core 9 |
| Database | Microsoft SQL Server (Express) |
| Authentication | JSON Web Token (JWT Bearer) |
| Password Hashing | BCrypt.Net-Next 4 |
| PDF Generation | QuestPDF 2025 |
| Email | MailKit 4 (Gmail SMTP) |
| WhatsApp | Twilio SDK 7 |
| Logging | Serilog (console + rolling file) |
| API Docs | Swagger / Swashbuckle |

### Frontend

| Layer | Technology |
|---|---|
| Framework | Angular 21 (standalone components) |
| Language | TypeScript 5.9 |
| UI Library | Angular Material 21 + Bootstrap 5.3 |
| Icons | Bootstrap Icons 1.13 |
| HTTP | Angular HttpClient with functional interceptors |
| Routing | Angular Router with lazy loading + guards |

---

## System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Angular 21 Frontend                     │
│   (Standalone Components · Bootstrap 5 · Angular Material)  │
│                                                             │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────────┐  │
│  │ AuthGuard│ │ RoleGuard│ │  Error   │ │   Services   │  │
│  │          │ │          │ │Interceptor│ │ (HttpClient) │  │
│  └──────────┘ └──────────┘ └──────────┘ └──────────────┘  │
└───────────────────────────┬─────────────────────────────────┘
                            │ HTTPS / REST (JSON)
                            │ JWT Bearer Token
┌───────────────────────────▼─────────────────────────────────┐
│               ASP.NET Core 10 Web API                       │
│                                                             │
│  GlobalExceptionMiddleware → Auth → Authorization           │
│                                                             │
│  ┌────────────┐ ┌────────────┐ ┌────────────────────────┐  │
│  │Controllers │ │  Services  │ │  EF Core DbContext     │  │
│  │ (19 total) │ │Receipt/    │ │  (DsmsDbContext)       │  │
│  │            │ │Email/      │ │                        │  │
│  │            │ │WhatsApp/   │ └──────────┬─────────────┘  │
│  │            │ │Settings    │            │                 │
│  └────────────┘ └────────────┘            │                 │
└───────────────────────────────────────────┼─────────────────┘
                                            │ EF Core
                                ┌───────────▼──────────┐
                                │   SQL Server Express  │
                                │      (DSMS DB)        │
                                └──────────────────────┘
```

---

## Features

### Authentication & Security
- JWT-based login with configurable token expiry (default 8 hours)
- BCrypt password hashing
- Role-based route guards on both frontend (Angular guards) and backend (`[Authorize(Roles="...")]`)
- Branch-level data isolation — branch staff can only access their own branch's data
- First-time login flag to prompt password change

### Student Management
- Full student registration with personal details, NIC, special requirements, and vehicle class selection
- Student profile with tabbed view: Info, Payment History, Documents, Training Progress
- Document upload (NIC, medical certificate, passport photo) stored as base64 with file type validation
- Student search and filtering by name, NIC, or status

### Billing & Payments
- Create bills linked to a student's registered course package
- Instalment-based payments — each payment generates a unique sequential bill number
- Running balance automatically computed against the course package price
- Payment history with receipt download (PDF via QuestPDF)
- Pending payments dashboard showing overdue balances
- Automated receipt delivery by email and WhatsApp on each payment

### Training Schedule
- Training sessions created per student per vehicle class
- Dynamic session count driven by the `TrainingDays` field on each course package
- Session status tracking: Scheduled, Completed, Cancelled
- Instructor-linked sessions with vehicle class assignment
- Booking form with instructor and date selection

### Instructor Management
- Instructor CRUD with branch assignment and license details
- Instructor dashboard showing assigned students and upcoming sessions
- Attendance marking — instructors mark each training session as completed or absent
- Enrolment management — link students to instructors per vehicle class

### Exam Management
- Written exam result entry with score and pass/fail recording
- Practical test attempt tracking — supports multiple attempts per student
- Automatic license number and issue date recording on a successful practical test pass
- Student progress overview by vehicle class

### Reports & Analytics
- Income reports with daily, weekly, monthly, and custom date range options
- Student registration summary by branch and vehicle class
- Exam pass/fail statistics
- Daily income bar chart for visual trend analysis

### Administration
- **Branch management** — create and manage branches with unique codes
- **User management** — create staff accounts, assign roles and branch access
- **Course package management** — configure packages with pricing and training day count per vehicle class
- **Employee management** — CRUD for administrative staff records
- **System settings** — school name, address, contact details used across receipts and emails

### Notifications
- Email receipts sent automatically via Gmail SMTP (MailKit) on each payment
- WhatsApp receipts delivered via Twilio Messaging API
- Enrolment confirmation emails to newly registered students

### Error Handling & Reliability
- `GlobalExceptionMiddleware` catches all unhandled exceptions and returns structured JSON error responses
- SQL Server foreign key and unique constraint violations translated to human-readable messages
- Angular `ErrorInterceptor` normalises HTTP errors and triggers automatic logout on 401 responses
- Serilog structured logging — daily rolling log files (`Logs/dsms-YYYY-MM-DD.log`), retained 30 days
- Startup database connection check — logs clearly if SQL Server is unreachable

---

## User Roles & Permissions

| Role | Scope | Key Access |
|---|---|---|
| **Company Admin** | All branches | Full system access — branches, users, packages, all reports |
| **Branch Admin** | Assigned branch | Students, billing, training, exams, staff within their branch |
| **Staff / OfficeStaff** | Assigned branch | Student registration, billing, document handling |
| **Instructor** | Assigned branch | Own dashboard, attendance marking, session view |

---

## Project Structure

```
Final Year - DSMS/
├── DSMS-Backend/
│   └── DSMS.API/
│       ├── Controllers/          # 19 REST API controllers
│       │   ├── AuthController.cs
│       │   ├── StudentController.cs
│       │   ├── BillingController.cs
│       │   ├── TrainingSessionController.cs
│       │   ├── TrainingAttendanceController.cs
│       │   ├── InstructorController.cs
│       │   ├── ExamResultController.cs
│       │   ├── PracticalTestController.cs
│       │   ├── ReportsController.cs
│       │   ├── BranchController.cs
│       │   ├── CoursePackageController.cs
│       │   ├── UserManagementController.cs
│       │   ├── EmployeeController.cs
│       │   ├── DashboardController.cs
│       │   ├── SystemSettingsController.cs
│       │   ├── StudentDocumentController.cs
│       │   ├── LookupController.cs
│       │   └── PayHereController.cs
│       ├── Data/
│       │   └── DsmsDbContext.cs  # EF Core DbContext with full entity config
│       ├── DTOs/
│       │   └── AuthDTOs.cs
│       ├── Helpers/
│       │   └── ClaimsHelper.cs   # JWT claims extraction utilities
│       ├── Middleware/
│       │   └── GlobalExceptionMiddleware.cs
│       ├── Migrations/           # EF Core migrations
│       ├── Models/               # 23 domain entity models
│       ├── Services/             # Receipt, Email, WhatsApp, SystemSettings
│       ├── appsettings.json      # Public config (no credentials)
│       ├── runtime-settings.json # Secrets (gitignored — never committed)
│       └── Program.cs            # App bootstrap, DI, middleware pipeline
│
└── DSMS-Frontend/
    └── src/app/
        ├── guards/
        │   └── auth.ts           # authGuard + roleGuard
        ├── interceptors/
        │   └── error.interceptor.ts
        ├── services/             # auth, student, billing, dashboard, employee
        ├── shared/
        │   ├── layout/           # sidebar, topbar layout wrappers
        │   ├── navbar/           # top navigation bar component
        │   └── sidebar/          # role-aware sidebar navigation
        └── pages/
            ├── login/
            ├── dashboard/
            ├── students/         # student-list, student-form, student-profile
            ├── billing/          # billing-list, billing-form, pending-payments
            ├── training/         # training-schedule, training-booking
            ├── instructor/       # instructor-dashboard, attendance
            ├── exam/             # exam (written), practical-tests
            ├── employees/        # employee-list, employee-form
            └── admin/            # branches, users, course-packages,
                                  # instructors, reports, settings
```

---

## Prerequisites

| Requirement | Version |
|---|---|
| .NET SDK | 10.0 or later |
| SQL Server | Express 2019+ (or any edition) |
| Node.js | 20 LTS or later |
| npm | 11+ |
| Angular CLI | 21 (`npm install -g @angular/cli`) |

---

## Getting Started

### Backend Setup

**1. Clone the repository**
```bash
git clone https://github.com/sanchithaarampath/Driving-School-Management-System.git
cd "Driving-School-Management-System/DSMS-Backend/DSMS.API"
```

**2. Create your local secrets file**

Create `appsettings.Local.json` (or `runtime-settings.json`) beside `appsettings.json`. This file is gitignored and holds real credentials:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=DSMS;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Email": {
    "SenderEmail": "your-gmail@gmail.com",
    "AppPassword": "your-gmail-app-password"
  },
  "Twilio": {
    "AccountSid": "ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
    "AuthToken": "your-auth-token",
    "WhatsAppFrom": "whatsapp:+14155238886"
  }
}
```

**3. Apply database migrations**
```bash
dotnet ef database update
```

This creates the `DSMS` database and all tables. On first run, the application automatically seeds:
- Default roles (Company Admin, Branch Admin, Staff, Instructor, etc.)
- A `Main Branch` record
- An initial admin user: **username** `admin` / **password** `Admin@1234`

> **Important:** Change the admin password immediately after first login.

**4. Run the API**
```bash
dotnet run
```

The API starts at `https://localhost:7xxx` (port shown in terminal).  
Swagger UI is available at `/swagger`.

---

### Frontend Setup

**1. Install dependencies**
```bash
cd DSMS-Frontend
npm install
```

**2. Start the development server**
```bash
ng serve
```

Open `http://localhost:4200` in your browser. The Angular app proxies API calls to the backend automatically.

---

## Environment Configuration

All sensitive values are kept **out of source control**. The `appsettings.json` committed to the repository contains only placeholder values for external services.

| Setting | Where to configure | Notes |
|---|---|---|
| Database connection string | `runtime-settings.json` | Real server/credentials |
| Gmail SMTP credentials | `runtime-settings.json` | Use a Gmail App Password |
| Twilio Account SID / Auth Token | `runtime-settings.json` | WhatsApp receipt delivery |
| JWT Secret Key | `appsettings.json` | Change in production |
| School name / address / phone | Admin → Settings (UI) | Stored in DB, used on receipts |

Files protected by `.gitignore`:
```
**/runtime-settings.json
**/appsettings.Local.json
**/Logs/
**/Uploads/student-docs/
bin/
obj/
```

---

## API Overview

All endpoints (except `/api/auth/login`) require a `Bearer` JWT token in the `Authorization` header.

| Controller | Base Route | Responsibility |
|---|---|---|
| `AuthController` | `/api/auth` | Login, token issuance, admin setup |
| `StudentController` | `/api/students` | Student CRUD with branch filtering |
| `StudentDocumentController` | `/api/student-documents` | Document upload, retrieval, deletion |
| `BillingController` | `/api/billing` | Bills, instalment payments, balance |
| `TrainingSessionController` | `/api/training-sessions` | Session scheduling and management |
| `TrainingAttendanceController` | `/api/training-attendance` | Enrolment and attendance marking |
| `InstructorController` | `/api/instructors` | Instructor CRUD |
| `ExamResultController` | `/api/exam-results` | Written exam score recording |
| `PracticalTestController` | `/api/practical-tests` | Test attempts and license recording |
| `ReportsController` | `/api/reports` | Income, registration, exam reports |
| `BranchController` | `/api/branches` | Branch management (Company Admin) |
| `CoursePackageController` | `/api/course-packages` | Package and training day config |
| `UserManagementController` | `/api/users` | Staff account management |
| `EmployeeController` | `/api/employees` | Employee records |
| `DashboardController` | `/api/dashboard` | KPI data per role |
| `SystemSettingsController` | `/api/settings` | School info for receipts/emails |
| `LookupController` | `/api/lookup` | Dropdown reference data |

Full interactive documentation is available via Swagger UI at `/swagger` when the API is running.

---

## Database Schema

The database contains the following primary entities and their relationships:

```
Branches ──< Users (UserSecurity)
          ──< Students ──< StudentVehicleClass
                        ──< StudentDocument
                        ──< StudentPackageRegistration ──> CoursePackage
                        ──< Bills ──< Payments
                        ──< TrainingAttendance ──> TrainingSessions
                        ──< StudentClassProgress
                        ──< StudentPracticalTestAttempt

CoursePackage ──< PackageVehicleClass ──> VehicleClass
Branches ──< Instructors
Branches ──< TrainingSessions ──> Instructors
Roles ──< UserSecurity
```

**Key design decisions:**
- Every `Bill` stores its own running `RunningBalance` calculated server-side to prevent double-counting
- Each `Payment` generates a unique sequential `BillNumber` within its branch
- `StudentPracticalTestAttempt` records every attempt; the `Passed` flag on `Student` is set when any attempt passes
- `TrainingSession.TrainingDays` is driven by `CoursePackage.TrainingDays` ensuring consistency across enrolments

---

## Development Workflow

The repository follows a feature-branch development model:

```
main               ← stable release
  └── development  ← integration branch
        ├── feature/initial-setup
        ├── feature/authentication
        ├── feature/student-management
        ├── feature/billing
        ├── feature/training-schedule
        ├── feature/instructor-management
        ├── feature/exam-management
        ├── feature/reports-analytics
        ├── feature/admin-management
        ├── feature/notifications
        └── feature/error-handling
```

Each feature branch represents a discrete development phase. Feature branches are merged into `development` via merge commits, and `development` is merged to `main` for release.

---

*Developed by **Sanchitha Arampath** | Plymouth Index Number: 10952572*  
*Supervisor: Dharanai Rajasinghe | University of Plymouth — 2026*
