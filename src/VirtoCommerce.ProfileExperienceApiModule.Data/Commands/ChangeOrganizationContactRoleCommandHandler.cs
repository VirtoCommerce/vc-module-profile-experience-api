using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models;
using VirtoCommerce.ProfileExperienceApiModule.Data.Queries;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class ChangeOrganizationContactRoleCommandHandler : UserCommandHandlerBase, IRequestHandler<ChangeOrganizationContactRoleCommand, IdentityResultResponse>
    {
        private readonly Func<RoleManager<Role>> _roleManagerFactory;
        private readonly IOrganizationMembershipService _organizationMembershipService;
        private readonly IOrganizationMembershipSearchService _organizationMembershipSearchService;
        private readonly IContactAggregateRepository _contactAggregateRepository;

        public ChangeOrganizationContactRoleCommandHandler(
            Func<UserManager<ApplicationUser>> userManager,
            Func<RoleManager<Role>> roleManagerFactory,
            IOrganizationMembershipService organizationMembershipService,
            IOrganizationMembershipSearchService organizationMembershipSearchService,
            IContactAggregateRepository contactAggregateRepository,
            IOptions<AuthorizationOptions> securityOptions)
            : base(userManager, securityOptions)
        {
            _roleManagerFactory = roleManagerFactory;
            _organizationMembershipService = organizationMembershipService;
            _organizationMembershipSearchService = organizationMembershipSearchService;
            _contactAggregateRepository = contactAggregateRepository;
        }

        public async Task<IdentityResultResponse> Handle(ChangeOrganizationContactRoleCommand request, CancellationToken cancellationToken)
        {
            var result = new IdentityResultResponse
            {
                Errors = new List<IdentityErrorInfo>(),
                Succeeded = false,
            };

            if (string.IsNullOrEmpty(request.OrganizationId))
            {
                result.Errors.Add(new IdentityErrorInfo { Code = "OrganizationIdRequired", Description = "OrganizationId is required for organization-scoped role assignment." });
                return result;
            }

            var contactAggregate = await _contactAggregateRepository.GetMemberAggregateRootByIdAsync<ContactAggregate>(request.MemberId)
                ?? throw new InvalidOperationException($"Contact '{request.MemberId}' not found.");

            var userId = contactAggregate.Contact?.SecurityAccounts?.FirstOrDefault()?.Id;
            if (string.IsNullOrEmpty(userId))
            {
                result.Errors.Add(new IdentityErrorInfo { Code = "Forbidden", Description = "It is forbidden to edit this user." });
                return result;
            }

            using var userManager = _userManagerFactory();
            var user = await userManager.FindByIdAsync(userId);
            if (user == null || !IsUserEditable(user.UserName))
            {
                result.Errors.Add(new IdentityErrorInfo { Code = "Forbidden", Description = "It is forbidden to edit this user." });
                return result;
            }

            using var roleManager = _roleManagerFactory();
            var roles = new List<Role>();
            foreach (var roleId in request.RoleIds ?? [])
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

            var searchResult = await _organizationMembershipSearchService.SearchAsync(
                new OrganizationMembershipSearchCriteria
                {
                    UserId = userId,
                    OrganizationId = request.OrganizationId,
                    Take = 1
                });

            var membership = searchResult.Results.FirstOrDefault();
            if (membership == null)
            {
                result.Errors.Add(new IdentityErrorInfo
                {
                    Code = "MembershipNotFound",
                    Description = $"Contact '{request.MemberId}' has no membership in organization '{request.OrganizationId}'.",
                });
                return result;
            }

            membership.Roles = roles
                .Select(r => new OrganizationMembershipRole
                {
                    MembershipId = membership.Id,
                    RoleId = r.Id,
                    RoleName = r.Name,
                })
                .ToList();

            await _organizationMembershipService.SaveChangesAsync([membership]);

            result.Succeeded = true;
            return result;
        }
    }
}
