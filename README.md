# CRManager - Credit Card Debt Intelligence & Expense Management

A modern, full-stack portfolio management solution built with **.NET 8**, **ASP.NET Core Web API**, **Entity Framework Core (SQL Server)**, and **Blazor / .NET MAUI Hybrid**.

---

## 🚀 How to Run in Visual Studio 2022 (F5 One-Click)

### 1. Prerequisites
- **Visual Studio 2022** (v17.8 or newer) with:
  - **ASP.NET and web development** workload.
  - (Optional for Desktop/Mobile) **.NET Multi-platform App UI development** workload.
- **SQL Server** (Local instance, LocalDB, or Docker container).

---

### 2. Configure Database Connection String
Open `src/CRManager.Api/appsettings.json` and set your SQL Server connection:

- **If using Local SQL Server with Windows Authentication:**
  ```json
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=CRManagerDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;"
  }
  ```
- **If using SQL Server Authentication (or Docker):**
  ```json
  "ConnectionStrings": {
    "DefaultConnection": "Server=127.0.0.1,1433;Database=CRManagerDb;User Id=sa;Password=YourStrongPassword123!;TrustServerCertificate=True;MultipleActiveResultSets=True;"
  }
  ```

> 💡 **Automatic Database Creation**: When `CRManager.Api` runs, EF Core will **automatically apply all migrations and create all tables** on startup.

---

### 3. Open & Run Multiple Projects Simultaneously
1. Open `CRManager.sln` in **Visual Studio 2022**.
2. Right-click the Solution `CRManager` in **Solution Explorer** → **Configure Startup Projects...**
3. Select **Multiple startup projects**:
   - `CRManager.Api` ➔ **Start**
   - `CRManager.Client.Web` (or `CRManager.Client.Maui`) ➔ **Start**
4. Click **Apply** and press **F5** (or click the green Play button).

Visual Studio will start the API backend ([http://localhost:5283/swagger](http://localhost:5283/swagger)) and launch the Blazor frontend ([http://localhost:5260](http://localhost:5260)) together.
