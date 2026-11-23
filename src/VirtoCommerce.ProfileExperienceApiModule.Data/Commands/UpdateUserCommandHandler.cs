using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Security;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class UpdateUserCommandHandler : UserCommandHandlerBase, IRequestHandler<UpdateUserCommand, IdentityResult>
    {
        public UpdateUserCommandHandler(Func<UserManager<ApplicationUser>> userManager, IOptions<AuthorizationOptions> securityOptions)
            : base(userManager, securityOptions)
        {
        }

        public virtual async Task<IdentityResult> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            using var userManager = _userManagerFactory();

            var storedUser = await userManager.FindByIdAsync(request.ApplicationUser.Id);
            if (storedUser is null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "User not found." });
            }

            if (!IsUserEditable(storedUser.UserName) || !IsUserEditable(request.ApplicationUser.UserName))
            {
                return IdentityResult.Failed(new IdentityError { Description = "It is forbidden to edit this user." });
            }

            if (request.CanUpdateSecurityData)
            {
                return await userManager.UpdateAsync(request.ApplicationUser);
            }

            var userToSave = GetUserWithProfileChangesOnly(storedUser, request.ApplicationUser);

            return storedUser.Email.EqualsIgnoreCase(userToSave.Email)
                ? await userManager.UpdateAsync(userToSave)
                : await userManager.SetEmailAsync(userToSave, userToSave.Email);
        }

        /// <summary>
        /// Takes the stored user and applies only the profile fields a user may change without security permissions.
        /// Everything else (roles, user type, password hash, security stamp, member, store, lockout, 2FA) keeps its stored value.
        /// </summary>
        protected virtual ApplicationUser GetUserWithProfileChangesOnly(ApplicationUser storedUser, ApplicationUser requestedUser)
        {
            // The stored user can come from the user cache, so never change it in place
            var result = storedUser.CloneTyped();

            if (!result.Email.EqualsIgnoreCase(requestedUser.Email))
            {
                result.EmailConfirmed = false;
            }

            result.Email = requestedUser.Email;
            result.PhoneNumber = requestedUser.PhoneNumber;
            result.PhotoUrl = requestedUser.PhotoUrl;

            // null means "do not synchronize" in CustomUserManager.UpdateAsync
            result.Roles = null;
            result.Logins = null;

            return result;
        }
    }
}
