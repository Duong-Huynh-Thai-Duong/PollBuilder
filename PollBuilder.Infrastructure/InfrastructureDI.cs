using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PollBuilder.Application.Interfaces; // <-- Add this using statement
using PollBuilder.Infrastructure.Data;
using PollBuilder.Infrastructure.Identity;
using PollBuilder.Infrastructure.Services; // <-- Add this using statement

namespace PollBuilder.Infrastructure
{
    public static class InfrastructureDI
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // 1. Setup SQL Server Connection
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            // 2. Setup Identity Framework
            services.AddDefaultIdentity<ApplicationUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>();

            // 3. Register Application Services
            services.AddScoped<IPollService, PollService>(); // <-- New Registration

            return services;
        }
    }
}