using Microsoft.AspNetCore.Identity;
using VirtoCommerce.ExperienceApiModule.Core.Infrastructure;
using VirtoCommerce.Platform.Core.Security;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class UpdateUserCommand : ICommand<IdentityResult>
    {
        public ApplicationUser ApplicationUser { get; set; } = new ApplicationUser();

        /// <summary>
        /// Whether the caller may change security data (roles, user type, password hash, security stamp, member, store, lockout, etc.).
        /// It is not part of the GraphQL input. ProfileAuthorizationHandler sets it from the caller's permissions.
        /// When false, only the user's own email, phone number and photo URL are updated.
        /// </summary>
        public bool CanUpdateSecurityData { get; set; }
    }
}
