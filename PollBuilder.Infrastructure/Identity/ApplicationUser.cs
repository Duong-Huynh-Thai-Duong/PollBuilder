using Microsoft.AspNetCore.Identity;

namespace PollBuilder.Infrastructure.Identity
{
    // Inherits all default security features (Password hashes, 2FA, etc.)
    public class ApplicationUser : IdentityUser
    {
        // You can add custom properties here later (e.g., string FullName)
        // For now, the default Identity fields are perfect for the Merit grade.
    }
}