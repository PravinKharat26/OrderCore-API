# Local Development & Laptop Transfer Guide

This document provides operational instructions for working on the **Order Management System** on your current work laptop and subsequently transferring it to your personal laptop for Dockerization.

---

## 1. Work Laptop Development (Zero IT Impact)

### Safety Guarantees
- **No Docker Required**: Everything compiles and runs locally with the standard .NET 9 SDK.
- **No Database Server Required**: SQLite is an embedded library running entirely within the application process. No background Windows services or SQL server instances are started.
- **Project Isolation**: All files, databases (`*.db`), and build outputs are strictly kept inside your personal directory:
  `c:\Professional\Personal\OrderManagementSystem\OrderManagementSystem`
- **Port Safety**: The assigned ports (`5081`, `5082`, `5083`, `5080`) were verified to be unoccupied before binding.

---

## 2. Running Locally (Step-by-Step)

### Option A: Using Visual Studio / JetBrains Rider / VS Code
1. Open the solution file: `..\OrderManagementSystem.sln`.
2. Set multiple startup projects:
   - `IdentityService`
   - `InventoryService`
   - `OrderService`
3. Press **F5** or **Ctrl+F5**.

### Option B: Using Command Line (PowerShell)
Open three separate terminal windows:

```powershell
# Terminal 1 — IdentityService
cd c:\Professional\Personal\OrderManagementSystem\OrderManagementSystem\IdentityService
dotnet run --launch-profile http

# Terminal 2 — InventoryService
cd c:\Professional\Personal\OrderManagementSystem\OrderManagementSystem\InventoryService
dotnet run --launch-profile http

# Terminal 3 — OrderService
cd c:\Professional\Personal\OrderManagementSystem\OrderManagementSystem\OrderService
dotnet run --launch-profile http
```

### Accessing Swagger UIs
- **IdentityService**: `http://localhost:5081/swagger`
- **InventoryService**: `http://localhost:5082/swagger`
- **OrderService**: `http://localhost:5083/swagger`
- **System Gateway**: `http://localhost:5080/swagger`

---

## 3. How SQLite Databases Work

- SQLite databases are created automatically on application boot via `context.Database.EnsureCreatedAsync()`.
- Default database file locations:
  - `IdentityService/identity.db`
  - `InventoryService/inventory.db`
  - `OrderService/orders.db`
- To reset any database back to its initial seeded state, simply delete the corresponding `.db` file while the service is stopped; it will be recreated and reseeded automatically upon the next launch!

---

## 4. Transferring to Your Personal Laptop

### Step 1: Clean Build Artifacts
Before copying the project to a USB drive or private repository, remove temporary compiler outputs and local databases:

```powershell
cd c:\Professional\Personal\OrderManagementSystem\OrderManagementSystem
dotnet clean ..\OrderManagementSystem.sln
Get-ChildItem -Recurse -Include bin, obj, *.db | Remove-Item -Recurse -Force
```

### Step 2: Initialize Git on Personal Machine
Once transferred to your personal laptop:
```powershell
git init
git add .
git commit -m "feat: Initial commit of Order Management System microservices"
```
*(Ensure you do NOT push while on the work laptop or corporate VPN if company policy restricts personal Git remotes).*

### Step 3: Run with Docker Compose on Personal Laptop
Once Docker Desktop is installed on your personal laptop:
```powershell
docker-compose up --build
```
This will:
1. Compile each service using multi-stage .NET 9 SDK images.
2. Create isolated Linux containers for `identity-service`, `inventory-service`, and `order-service`.
3. Mount persistent Docker named volumes for SQLite database files (`/app/data/`).
4. Connect all services through `ordermanagement-network` with automatic container DNS.
5. Expose ports `5081`, `5082`, and `5083` to your personal host browser.
