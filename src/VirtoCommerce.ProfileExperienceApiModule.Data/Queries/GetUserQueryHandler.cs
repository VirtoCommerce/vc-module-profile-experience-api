using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Validators;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries
{
    public class GetUserQueryHandler : IQueryHandler<GetUserQuery, ApplicationUser>
    {
        private readonly Func<UserManager<ApplicationUser>> _userManagerFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IOrganizationMembershipService _organizationMembershipService;

        public GetUserQueryHandler(
            Func<UserManager<ApplicationUser>> userManager,
            IHttpContextAccessor httpContextAccessor,
            IOrganizationMembershipService organizationMembershipService)
        {
            _userManagerFactory = userManager;
            _httpContextAccessor = httpContextAccessor;
            _organizationMembershipService = organizationMembershipService;
        }

        public virtual async Task<ApplicationUser> Handle(GetUserQuery request, CancellationToken cancellationToken)
        {
            ApplicationUser result = default;

            await new UserQueryValidator().ValidateAndThrowAsync(request);

            using (var userManager = _userManagerFactory())
            {
                if (!request.Id.IsNullOrEmpty())
                {
                    result = await userManager.FindByIdAsync(request.Id);
                }
                else if (!request.LoginProvider.IsNullOrEmpty() && !request.ProviderKey.IsNullOrEmpty())
                {
                    result = await userManager.FindByLoginAsync(request.LoginProvider, request.ProviderKey);
                }
                else
                {
                    if (!request.UserName.IsNullOrEmpty())
                    {
                        result = await userManager.FindByNameAsync(request.UserName);
                    }
                    if (result == null && !request.Email.IsNullOrEmpty())
                    {
                        result = await userManager.FindByEmailAsync(request.Email);
                    }
                }
            }

            if (result != null)
            {
                await AugmentWithOrganizationRolesAsync(result);
            }

            return result;
        }

        protected virtual async Task AugmentWithOrganizationRolesAsync(ApplicationUser user)
        {
            var organizationId = _httpContextAccessor?.HttpContext?.User?.FindFirstValue("organization_id");

            if (string.IsNullOrEmpty(organizationId))
            {
                return;
            }

            var organizationMembershipSearchCriteria = new OrganizationMembershipSearchCriteria { UserId = user.Id };
            var searchResult = await _organizationMembershipService.SearchAsync(organizationMembershipSearchCriteria);
            var membership = searchResult?.Results?.FirstOrDefault(m => m.OrganizationId == organizationId);

            if (membership?.Roles is not { Count: > 0 })
            {
                return;
            }

            var roles = user.Roles ?? [];
            var existingRoleIds = roles.Select(r => r.Id).ToHashSet();
            var newRoles = membership.Roles
                .Where(mr => !existingRoleIds.Contains(mr.RoleId))
                .Select(mr => new Role
                {
                    Id = mr.RoleId,
                    Name = mr.RoleName
                });

            user.Roles = [.. roles, .. newRoles];
        }
    }
}
