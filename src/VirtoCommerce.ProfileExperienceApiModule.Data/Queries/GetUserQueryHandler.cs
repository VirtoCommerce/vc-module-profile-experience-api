using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Validators;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries
{
    public class GetUserQueryHandler : IQueryHandler<GetUserQuery, ApplicationUser>
    {
        private readonly Func<UserManager<ApplicationUser>> _userManagerFactory;

        public GetUserQueryHandler(Func<UserManager<ApplicationUser>> userManager)
        {
            _userManagerFactory = userManager;
        }

        public virtual async Task<ApplicationUser> Handle(GetUserQuery request, CancellationToken cancellationToken)
        {
            ApplicationUser result = default;

            await new UserQueryValidator().ValidateAndThrowAsync(request);

            using var userManager = _userManagerFactory();

            if (!string.IsNullOrEmpty(request.Id))
            {
                result = await userManager.FindByIdAsync(request.Id);
            }
            else if (!string.IsNullOrEmpty(request.LoginProvider) && !string.IsNullOrEmpty(request.ProviderKey))
            {
                result = await userManager.FindByLoginAsync(request.LoginProvider, request.ProviderKey);
            }
            else
            {
                if (!string.IsNullOrEmpty(request.UserName))
                {
                    result = await userManager.FindByNameAsync(request.UserName);
                }
                if (result == null && !string.IsNullOrEmpty(request.Email))
                {
                    result = await userManager.FindByEmailAsync(request.Email);
                }
            }

            return result;
        }
    }
}
