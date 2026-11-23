using Microsoft.AspNetCore.Identity;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class CreateUserCommand : ICommand<IdentityResult>
    {
        public ApplicationUser ApplicationUser { get; set; } = new ApplicationUser();

        /// <summary>
        /// Not part of the GraphQL input. ProfileAuthorizationHandler sets it from the caller's permissions.
        /// When false, only profile fields of the requested user are saved (no roles, logins, member, id or security flags).
        /// </summary>
        public bool CanSetSecurityData { get; set; }
    }
}
