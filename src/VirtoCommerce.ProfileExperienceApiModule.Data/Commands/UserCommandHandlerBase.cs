using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Security;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class UserCommandHandlerBase
    {
        protected readonly Func<UserManager<ApplicationUser>> _userManagerFactory;
        private readonly AuthorizationOptions _securityOptions;

        public UserCommandHandlerBase(Func<UserManager<ApplicationUser>> userManager, IOptions<AuthorizationOptions> securityOptions)
        {
            _userManagerFactory = userManager;
            _securityOptions = securityOptions.Value;
        }

        protected bool IsUserEditable(string userName)
        {
            return _securityOptions.NonEditableUsers?.FirstOrDefault(x => x.EqualsIgnoreCase(userName)) == null;
        }

        protected virtual Task<IdentityResult> SaveUserAsync(UserManager<ApplicationUser> userManager, ApplicationUser user, string storedEmail)
        {
            return storedEmail.EqualsIgnoreCase(user.Email)
                ? userManager.UpdateAsync(user)
                : userManager.SetEmailAsync(user, user.Email);
        }
    }
}
