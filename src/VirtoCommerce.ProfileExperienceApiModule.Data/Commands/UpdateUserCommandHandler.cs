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
            using (var userManager = _userManagerFactory())
            {
                var user = await userManager.FindByIdAsync(request.ApplicationUser.Id);
                if (user == null)
                {
                    return IdentityResult.Failed();
                }

                if (!IsUserEditable(user.UserName))
                {
                    return IdentityResult.Failed(new IdentityError { Description = "It is forbidden to edit this user." });
                }

                return await UpdateUserAsync(userManager, user, request.ApplicationUser);
            }
        }

        protected virtual async Task<IdentityResult> UpdateUserAsync(UserManager<ApplicationUser> userManager, ApplicationUser user, ApplicationUser changes)
        {
            var phoneNumber = changes.PhoneNumber.EmptyToNull();
            var photoUrl = changes.PhotoUrl.EmptyToNull();
            var isPhotoChanged = user.PhotoUrl.EmptyToNull() != photoUrl;

            if (isPhotoChanged)
            {
                user.PhotoUrl = photoUrl;
            }

            if (user.PhoneNumber.EmptyToNull() != phoneNumber)
            {
                return await userManager.SetPhoneNumberAsync(user, phoneNumber);
            }

            return isPhotoChanged
                ? await userManager.UpdateAsync(user)
                : IdentityResult.Success;
        }
    }
}
