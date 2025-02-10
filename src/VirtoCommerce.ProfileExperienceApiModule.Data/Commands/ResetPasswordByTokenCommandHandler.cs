using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Extensions;
using VirtoCommerce.ProfileExperienceApiModule.Data.Queries;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class ResetPasswordByTokenCommandHandler : UserCommandHandlerBase, IRequestHandler<ResetPasswordByTokenCommand, IdentityResultResponse>
    {
        public ResetPasswordByTokenCommandHandler(
            Func<UserManager<ApplicationUser>> userManagerFactory,
            IOptions<AuthorizationOptions> securityOptions)
            : base(userManagerFactory, securityOptions)
        {
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
            else if (!await userManager.HasPasswordAsync(user))
            {
                identityResult = IdentityResult.Failed(new IdentityError { Code = "PasswordNotResetable", Description = "You can't reset the password right now." });
            }
            else
            {
                identityResult = await userManager.ResetPasswordAsync(user, Uri.UnescapeDataString(request.Token), request.NewPassword);

                if (identityResult.Succeeded && user.PasswordExpired)
                {
                    user.PasswordExpired = false;
                    await userManager.UpdateAsync(user);
                }
            }

            result.Errors = identityResult?.Errors.Select(x => x.MapToIdentityErrorInfo()).ToList();
            result.Succeeded = identityResult?.Succeeded ?? false;

            return result;
        }
    }
}
