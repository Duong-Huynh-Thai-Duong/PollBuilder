# ThruSub (PollBuilder)

## Project Description
ThruSub is a modern, responsive web application designed for creating, sharing, and analyzing custom polls. Built with a clean, Pinterest-inspired user interface, it provides a seamless experience for both poll creators and voters. 

**Key Features:**
*   **Dynamic Poll Creation:** Create polls with various question types (Multiple Choice, Yes/No, 1-5 Star Ratings, and Open Text).
*   **Secure Voting:** Enforces a strict "one-vote-per-person" policy using secure browser cookies to prevent duplicate submissions.
*   **Creator Security:** Integrates ASP.NET Core Identity for secure account management, featuring a strict 30-minute session timeout for inactive creators.
*   **Responsive Design:** Fully optimized for both desktop and mobile devices.

---

## Architecture Description
This application is built using the **Clean Architecture** pattern in ASP.NET Core. By separating our core business rules from the user interface, the application remains highly maintainable and testable.

1.  **Domain Layer (`PollBuilder.Domain`):** 
    Contains the core enterprise logic and entities (`Poll`, `Question`, `Option`, `Vote`) without any dependencies on external frameworks or databases.
2.  **Application Layer (`PollBuilder.Application`):** 
    Defines the business logic, interfaces (`IPollService`), and Data Transfer Objects (DTOs) used to pass data between layers.
3.  **Infrastructure Layer (`PollBuilder.Infrastructure`):** 
    Implements the interfaces defined in the Application layer, manages the Entity Framework Core `ApplicationDbContext`, handles database migrations, and configures Identity services.
4.  **Presentation Layer (`PollBuilder.MVC`):** 
    The front-facing ASP.NET Core MVC web application. It consumes the core application logic directly via dependency injection to render Razor HTML pages, handle form submissions, and manage the UI/UX styling.

---

## Setup Instructions

Follow these steps to run the application locally on your machine.

### Prerequisites
*   [.NET SDK](https://dotnet.microsoft.com/download) (Version 7.0 or 8.0)
*   [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (or LocalDB)
*   Visual Studio 2022, VS Code, or Rider

### Installation Steps

1.  **Clone the repository:**
    ```bash
    git clone [https://github.com/Duong-Huynh-Thai-Duong/PollBuilder.git](https://github.com/Duong-Huynh-Thai-Duong/PollBuilder.git)
    cd PollBuilder
    ```

2.  **Configure the Database Connection:**
    Open `PollBuilder.MVC/appsettings.json` and ensure your `DefaultConnection` string points to your local SQL Server instance.
    ```json
    "ConnectionStrings": {
      "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ThruSubDb;Trusted_Connection=True;MultipleActiveResultSets=true"
    }
    ```

3.  **Apply Entity Framework Migrations:**
    Open your terminal in the root directory (or use the Package Manager Console in Visual Studio) and run:
    ```bash
    dotnet ef database update --project PollBuilder.Infrastructure --startup-project PollBuilder.MVC
    ```

4.  **Run the Application:**
    ```bash
    cd PollBuilder.MVC
    dotnet run
    ```
5.  **Access the App:** 
    Open your browser and navigate to the localhost port specified in your console output (e.g., `https://localhost:7107`).

---

## Live Deployment

The application is containerized using Docker and actively hosted on Render. 

**Live Link:** [Insert  Render deployment URL here]
