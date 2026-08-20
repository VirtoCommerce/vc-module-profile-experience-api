using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Model.Search;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries;

public class GetOrganizationContactRolesQueryHandler : IQueryHandler<GetOrganizationContactRolesQuery, IList<Role>>
{
    private readonly IMemberService _memberService;
    private readonly IMemberSearchService _memberSearchService;
    private readonly IOrganizationMembershipSearchService _organizationMembershipService;
    private readonly Func<RoleManager<Role>> _roleManagerFactory;
    private readonly Func<UserManager<ApplicationUser>> _userManagerFactory;

    public GetOrganizationContactRolesQueryHandler(
        IMemberService memberService,
        IMemberSearchService memberSearchService,
        IOrganizationMembershipSearchService organizationMembershipService,
        Func<RoleManager<Role>> roleManagerFactory,
        Func<UserManager<ApplicationUser>> userManagerFactory)
    {
        _memberService = memberService;
        _memberSearchService = memberSearchService;
        _organizationMembershipService = organizationMembershipService;
        _roleManagerFactory = roleManagerFactory;
        _userManagerFactory = userManagerFactory;
    }

    public virtual async Task<IList<Role>> Handle(GetOrganizationContactRolesQuery request, CancellationToken cancellationToken)
    {
        var orgId = request.OrganizationId;

        var membershipsTask = _organizationMembershipService.SearchAllNoCloneAsync(
            new OrganizationMembershipSearchCriteria { OrganizationId = orgId });
        var contactsTask = _memberSearchService.SearchAllAsync(
            new MembersSearchCriteria { MemberId = orgId });
        var organizationTask = _memberService.GetByIdAsync(orgId, memberType: nameof(Organization));

        await Task.WhenAll(membershipsTask, contactsTask, organizationTask);

        var allOrgMemberships = membershipsTask.Result;

        var membershipRoles = allOrgMemberships
            .SelectMany(m => m.Roles ?? [])
            .Where(r => !string.IsNullOrEmpty(r.RoleId))
            .Select(r => new Role { Id = r.RoleId, Name = r.RoleName, NormalizedName = r.RoleName?.ToUpperInvariant() });

        var organization = organizationTask.Result as Organization;
        var orgLevelRoles = (organization?.Roles ?? [])
            .Where(r => !string.IsNullOrEmpty(r.RoleId))
            .Select(r => new Role { Id = r.RoleId, Name = r.RoleName, NormalizedName = r.RoleName?.ToUpperInvariant() });

        var orgUsers = await OrganizationUsersResolver.GetOrganizationUsersAsync(
            allOrgMemberships, contactsTask.Result, _userManagerFactory);

        var globalRolesByUser = await GlobalRolesResolver.GetGlobalRolesByUserAsync(
            orgUsers.Select(u => u.Id).ToList(), _roleManagerFactory, _userManagerFactory);
        var globalRoles = globalRolesByUser.Values.SelectMany(r => r);

        return membershipRoles
            .Concat(orgLevelRoles)
            .Concat(globalRoles)
            .GroupBy(r => r.Id, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
    }
}
