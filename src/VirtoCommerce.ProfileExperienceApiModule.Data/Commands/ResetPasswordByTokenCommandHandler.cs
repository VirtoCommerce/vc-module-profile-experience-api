using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Extensions;
using VirtoCommerce.ProfileExperienceApiModule.Data.Queries;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class ResetPasswordByTokenCommandHandler : UserCommandHandlerBase, IRequestHandler<ResetPasswordByTokenCommand, IdentityResultResponse>
    {
        private readonly Func<(IUserSessionsService SessionService, IServiceScope Scope)> _userSessionsServiceFactory;

        public ResetPasswordByTokenCommandHandler(
            Func<UserManager<ApplicationUser>> userManagerFactory,
            IOptions<AuthorizationOptions> securityOptions,
            Func<(IUserSessionsService SessionService, IServiceScope Scope)> userSessionsServiceFactory)
            : base(userManagerFactory, securityOptions)
        {
            _userSessionsServiceFactory = userSessionsServiceFactory;
        }

        public virtual async Task<IdentityResultResponse> Handle(ResetPasswordByTokenCommand request, CancellationToken cancellationToken)
        {
            var result = new IdentityResultResponse();
            IdentityResult identityResult;

            using var userManager = _userManagerFactory();

            var user = await userManager.FindByIdAsync(request.UserId);
            if (user is null)
            {
                identityResult = IdentityResult.Failed(new IdentityError { Code = "UserNotFound", Description = "User not found" });
            }
            else if (!IsUserEditable(user.UserName))
            {
                identityResult = IdentityResult.Failed(new IdentityError { Code = "UserNotEditable", Description = "It is forbidden to edit this user." });
            }
            else if (user.LockoutEnd > DateTime.UtcNow)
            {
                identityResult = IdentityResult.Failed(new IdentityError { Code = "UserLocked", Description = "It is forbidden to reset password for this user." });
            }
            else
            {
                identityResult = await userManager.ResetPasswordAsync(user, Uri.UnescapeDataString(request.Token), request.NewPassword);

                if (identityResult.Succeeded && user.PasswordExpired)
                {
                    user.PasswordExpired = false;
                    await userManager.UpdateAsync(user);
                }

                if (identityResult.Succeeded)
                {
                    // terminate all sessions for password reset request
                    await TerminateAllUserSessions(user.Id);
                }
            }

            result.Errors = identityResult?.Errors.Select(x => x.MapToIdentityErrorInfo()).ToList();
            result.Succeeded = identityResult?.Succeeded ?? false;

            return result;
        }

        private async Task TerminateAllUserSessions(string userId)
        {
            var (SessionService, Scope) = _userSessionsServiceFactory();
            using var scope = Scope;
            await SessionService.TerminateAllUserSessions(userId);
        }
    }
}
