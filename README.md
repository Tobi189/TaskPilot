# TaskPilot

TaskPilot is a small task manager built with ASP.NET Core Razor Pages and PostgreSQL.  
Users can register, log in, and manage their own tasks (create, edit, delete, filter) on a simple dashboard.

---

## Features

- **User authentication**
  - Register with username, email, and password
  - Passwords are hashed using `Microsoft.AspNetCore.Identity` password hashing
  - Cookie-based login and logout
  - Dashboard is protected with `[Authorize]`

- **Task management**
  - Per-user tasks (each task belongs to the logged-in user)
  - Create tasks via a modal (“New Task” button)
  - Edit existing tasks via the same modal
  - Delete tasks from the task list

- **Task metadata**
  - Status: `pending`, `in_progress`, `done`
  - Priority: `Low (0)`, `Medium (1)`, `High (2)`
  - `created_at` and `updated_at` timestamps displayed in the UI

- **Filtering**
  - Filter tasks by **status**
  - Filter tasks by **priority**
  - Filters are applied server-side in SQL

---

## Tech Stack

- **Backend:** ASP.NET Core Razor Pages (C#)
- **Framework:** .NET (tested with a recent .NET SDK)
- **Database:** PostgreSQL
- **Data access:** [Npgsql](https://www.npgsql.org/) (PostgreSQL ADO.NET provider)
- **Auth / security:**
  - Cookie authentication
  - `Microsoft.AspNetCore.Identity` password hashing

---

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/en-us/download) (e.g. .NET 8)
- [PostgreSQL](https://www.postgresql.org/download/) running locally (or remotely)

---

## Database Schema

### Users table

```sql
CREATE TABLE users (
    id            SERIAL PRIMARY KEY,
    username      VARCHAR(50)  UNIQUE NOT NULL,
    email         VARCHAR(100) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL
);
```

### Tasks table

```sql
CREATE TABLE tasks (
    id         SERIAL PRIMARY KEY,
    user_id    INT          NOT NULL,
    title      VARCHAR(100) NOT NULL,
    content    TEXT,
    status     VARCHAR(20)  DEFAULT 'pending',
    priority   SMALLINT     DEFAULT 0,
    created_at TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP    DEFAULT CURRENT_TIMESTAMP
);
```

The application assumes:

- `tasks.user_id` refers to `users.id`
- Each logged-in user only sees and manipulates their own tasks (`WHERE user_id = @user_id` in queries)

You can optionally add a foreign key:

```sql
ALTER TABLE tasks
ADD CONSTRAINT fk_tasks_user
FOREIGN KEY (user_id) REFERENCES users(id)
ON DELETE CASCADE;
```

---

## PostgreSQL Setup Example

Create a database and a dedicated user (replace passwords as you like):

```sql
CREATE DATABASE taskpilot;

CREATE USER taskpilot_user WITH PASSWORD 'taskpilotuser';
GRANT ALL PRIVILEGES ON DATABASE taskpilot TO taskpilot_user;
```

Connect to `taskpilot` and run the schema SQL above for `users` and `tasks`.

If you use the example user/DB above, your connection string will look like:

```text
Host=localhost;Port=5433;Database=taskpilot;Username=taskpilot_user;Password=taskpilotuser
```

(Adjust `Port` if your PostgreSQL is running on the default 5432.)

---

## Configuration

The app uses a connection string named `Pg`.

### Option 1 – Local appsettings (simple/local-only)

In `appsettings.Development.json`:

```jsonc
{
  "ConnectionStrings": {
    "Pg": "Host=localhost;Port=5433;Database=taskpilot;Username=taskpilot_user;Password=taskpilotuser"
  }
}
```

> This is fine for local development, but **don’t commit real passwords** if the repo is public.

### Option 2 – User Secrets (recommended for public repo)

Leave a placeholder in `appsettings.json` (or `appsettings.Development.json`), then set the real value via user secrets:

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Pg" "Host=localhost;Port=5433;Database=taskpilot;Username=taskpilot_user;Password=taskpilotuser"
```

ASP.NET Core will then read `ConnectionStrings:Pg` from user secrets in development, and from environment variables or config in production.

---

## Running the App

Clone the repository and run:

```bash
git clone https://github.com/your-username/taskpilot.git
cd taskpilot

dotnet restore
dotnet run
```

By default, the app will start on a local HTTPS URL (e.g. `https://localhost:7xxx` depending on your launch settings).  
Open the URL in a browser.

---

## Usage

1. **Register** a new account on the Register page.
2. **Log in** using your credentials.
3. You’ll be redirected to the **Dashboard**:
   - Use the **+ New Task** button to open the modal and create a new task.
   - Each task row shows:
     - Title
     - Content
     - Status and priority badges
     - Created/updated timestamps
   - Click **Edit** on a task row to open the modal pre-filled and update it.
   - Click **Delete** to remove a task belonging to the current user.
4. On the left sidebar:
   - Select **Status** and/or **Priority**
   - Click **Apply** to filter tasks server-side.

All task operations are scoped to the logged-in user on the server.

---

## Possible Improvements / Ideas

- Sorting (by created date, updated date, or priority)
- Pagination for long task lists
- “Mark as done” quick action button
- Change password / account settings page
- Tagging or due dates for tasks
- Deploy to a cloud host (Railway, Render, Azure, etc.)

---

## License

Add a license here if you want (e.g. MIT).
