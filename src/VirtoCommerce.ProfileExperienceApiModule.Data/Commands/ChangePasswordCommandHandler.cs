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
    public class ChangePasswordCommandHandler : UserCommandHandlerBase, IRequestHandler<ChangePasswordCommand, IdentityResultResponse>
    {
        public ChangePasswordCommandHandler(
            Func<UserManager<ApplicationUser>> userManagerFactory,
            IOptions<AuthorizationOptions> securityOptions)
            : base(userManagerFactory, securityOptions)
        {
        }

        public async Task<IdentityResultResponse> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
            using var userManager = _userManagerFactory();

            var user = await userManager.FindByIdAsync(request.UserId);

            if (user is null)
            {
                return CreateResponse(IdentityResult.Failed(new IdentityError { Code = "UserNotFound", Description = "User not found" }));
            }

            if (!IsUserEditable(user.UserName))
            {
                return CreateResponse(IdentityResult.Failed(new IdentityError { Code = "UserNotEditable", Description = "It is forbidden to edit this user." }));
            }

            if (request.OldPassword == request.NewPassword)
            {
                return CreateResponse(IdentityResult.Failed(new IdentityError { Code = "SamePassword", Description = "New password is the same as old password. Please choose another one." }));
            }

            var result = await userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);

            if (result.Succeeded && user.PasswordExpired)
            {
                user.PasswordExpired = false;
                await userManager.UpdateAsync(user);
            }

            return CreateResponse(result);
        }

        private static IdentityResultResponse CreateResponse(IdentityResult identityResult)
        {
            return new IdentityResultResponse
            {
                Errors = identityResult?.Errors.Select(x => x.MapToIdentityErrorInfo()).ToList(),
                Succeeded = identityResult?.Succeeded ?? false
            };
        }
    }
}
