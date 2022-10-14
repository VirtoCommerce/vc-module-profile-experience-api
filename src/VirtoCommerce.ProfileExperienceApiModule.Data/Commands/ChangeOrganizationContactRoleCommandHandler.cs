using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Extensions;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models;
using VirtoCommerce.ProfileExperienceApiModule.Data.Queries;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class ChangeOrganizationContactRoleCommandHandler : UserCommandHandlerBase, IRequestHandler<ChangeOrganizationContactRoleCommand, IdentityResultResponse>
    {
        private readonly Func<RoleManager<Role>> _roleManagerFactory;

        public ChangeOrganizationContactRoleCommandHandler(
            Func<UserManager<ApplicationUser>> userManager,
            Func<RoleManager<Role>> roleManagerFactory,
            IOptions<AuthorizationOptions> securityOptions)
            : base(userManager, securityOptions)
        {
            _roleManagerFactory = roleManagerFactory;
        }

        public async Task<IdentityResultResponse> Handle(ChangeOrganizationContactRoleCommand request, CancellationToken cancellationToken)
        {
            var result = new IdentityResultResponse
            {
                Errors = new List<IdentityErrorInfo>(),
                Succeeded = false,
            };
            // Get the requested user
            using var userManager = _userManagerFactory();
            using var roleManager = _roleManagerFactory();
            var user = (await userManager.FindByIdAsync(request.UserId)).Clone() as ApplicationUser; // Clone required to update user later. Otherwise, the userManager replaces instance data in update
            if (user == null || !IsUserEditable(user.UserName))
            {
                result.Errors.Add(new IdentityErrorInfo { Description = "It is forbidden to edit this user." });
            }

            var roles = new List<Role>();

            // Check passed roles existence
            foreach (var roleId in request.RoleIds)
            {
                var role = await roleManager.FindByIdAsync(roleId) ?? await roleManager.FindByNameAsync(roleId);
                if (role != null)
                {
                    roles.Add(role);
                }
                else
                {
                    result.Errors.Add(new IdentityErrorInfo { Code = "Role not found", Description = $"Role '{roleId}' not found", Parameter = roleId });
                }
            }

            if (result.Errors.Count > 0)
            {
                return result;
            }

            user.Roles = roles;

            var assignResult = await userManager.UpdateAsync(user);
            result.Errors.AddRange(assignResult.Errors.Select(x => x.MapToIdentityErrorInfo()));
            result.Succeeded = result.Errors.Count == 0;

            return result;
        }
    }
}
