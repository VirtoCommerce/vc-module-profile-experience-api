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
        private readonly IContactAggregateRepository _contactAggregateRepository;

        public ChangeOrganizationContactRoleCommandHandler(
            Func<UserManager<ApplicationUser>> userManager,
            Func<RoleManager<Role>> roleManagerFactory,
            IOrganizationMembershipService organizationMembershipService,
            IContactAggregateRepository contactAggregateRepository,
            IOptions<AuthorizationOptions> securityOptions)
            : base(userManager, securityOptions)
        {
            _roleManagerFactory = roleManagerFactory;
            _organizationMembershipService = organizationMembershipService;
            _contactAggregateRepository = contactAggregateRepository;
        }

        public virtual async Task<IdentityResultResponse> Handle(ChangeOrganizationContactRoleCommand request, CancellationToken cancellationToken)
        {
            var result = new IdentityResultResponse
            {
                Errors = [],
                Succeeded = false,
            };

            if (string.IsNullOrEmpty(request.OrganizationId))
            {
                result.Errors.Add(new IdentityErrorInfo { Code = "OrganizationIdRequired", Description = "OrganizationId is required for organization-scoped role assignment." });
                return result;
            }

            var contactAggregate = await GetContactAggregate(request.MemberId)
                ?? throw new InvalidOperationException($"Contact '{request.MemberId}' not found.");

            var userId = GetSecurityAccountId(contactAggregate);
            if (string.IsNullOrEmpty(userId))
            {
                result.Errors.Add(new IdentityErrorInfo { Code = "Forbidden", Description = "It is forbidden to edit this user." });
                return result;
            }

            if (!await ValidateUserEditable(userId, result))
            {
                return result;
            }

            var roles = await ResolveRoles(request.RoleIds, result);
            if (result.Errors.Count > 0)
            {
                return result;
            }

            var membership = await GetMembership(userId, request.OrganizationId);
            if (membership == null)
            {
                result.Errors.Add(new IdentityErrorInfo
                {
                    Code = "MembershipNotFound",
                    Description = $"Contact '{request.MemberId}' has no membership in organization '{request.OrganizationId}'.",
                });
                return result;
            }

            await ApplyRoles(membership, roles, cancellationToken);

            result.Succeeded = true;
            return result;
        }

        protected virtual Task<ContactAggregate> GetContactAggregate(string memberId)
        {
            return _contactAggregateRepository.GetMemberAggregateRootByIdAsync<ContactAggregate>(memberId);
        }

        protected virtual string GetSecurityAccountId(ContactAggregate contactAggregate)
        {
            return contactAggregate.Contact?.SecurityAccounts?.FirstOrDefault()?.Id;
        }

        protected virtual async Task<bool> ValidateUserEditable(string userId, IdentityResultResponse result)
        {
            using var userManager = _userManagerFactory();
            var user = await userManager.FindByIdAsync(userId);
            if (user == null || !IsUserEditable(user.UserName))
            {
                result.Errors.Add(new IdentityErrorInfo { Code = "Forbidden", Description = "It is forbidden to edit this user." });
                return false;
            }

            return true;
        }

        protected virtual async Task<IList<Role>> ResolveRoles(string[] roleIds, IdentityResultResponse result)
        {
            using var roleManager = _roleManagerFactory();
            var roles = new List<Role>();
            foreach (var roleId in roleIds ?? [])
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

            return roles;
        }

        protected virtual Task<OrganizationMembership> GetMembership(string userId, string organizationId)
        {
            return _organizationMembershipService.GetByUserAndOrgAsync(userId, organizationId);
        }

        protected virtual IList<OrganizationMembershipRole> BuildMembershipRoles(OrganizationMembership membership, IList<Role> roles)
        {
            return roles
                .Select(r => new OrganizationMembershipRole
                {
                    MembershipId = membership.Id,
                    RoleId = r.Id,
                    RoleName = r.Name,
                })
                .ToList();
        }

        protected virtual async Task ApplyRoles(OrganizationMembership membership, IList<Role> roles, CancellationToken cancellationToken)
        {
            membership.Roles = BuildMembershipRoles(membership, roles);
            await _organizationMembershipService.SaveChangesAsync([membership]);
        }
    }
}
