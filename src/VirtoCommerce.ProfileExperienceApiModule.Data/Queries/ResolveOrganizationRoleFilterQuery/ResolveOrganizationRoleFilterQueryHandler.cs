using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Model.Search;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries;

public class ResolveOrganizationRoleFilterQueryHandler : IQueryHandler<ResolveOrganizationRoleFilterQuery, ContactIdFilterResult>
{
    private readonly IMemberService _memberService;
    private readonly IMemberSearchService _memberSearchService;
    private readonly IOrganizationMembershipSearchService _organizationMembershipService;
    private readonly Func<RoleManager<Role>> _roleManagerFactory;
    private readonly Func<UserManager<ApplicationUser>> _userManagerFactory;

    public ResolveOrganizationRoleFilterQueryHandler(
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

    public virtual async Task<ContactIdFilterResult> Handle(ResolveOrganizationRoleFilterQuery request, CancellationToken cancellationToken)
    {
        var orgId = request.OrganizationId;
        var roleIds = request.RoleIds;

        var roleIdsSet = roleIds.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var organization = await _memberService.GetByIdAsync(orgId, memberType: nameof(Organization)) as Organization;
        var orgRoleIdentifiers = organization?.Roles?
            .SelectMany(r => new[] { r.RoleId, r.RoleName })
            .Where(x => !string.IsNullOrEmpty(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];

        if (roleIdsSet.Overlaps(orgRoleIdentifiers))
        {
            return new ContactIdFilterResult { FilterRequired = false };
        }

        var membershipsTask = _organizationMembershipService.SearchAllNoCloneAsync(
            new OrganizationMembershipSearchCriteria { OrganizationId = orgId });
        var contactsTask = _memberSearchService.SearchAllAsync(
            new MembersSearchCriteria { MemberId = orgId });

        await Task.WhenAll(membershipsTask, contactsTask);

        var allOrgMemberships = membershipsTask.Result;
        var orgUsers = await OrganizationUsersResolver.GetOrganizationUsersAsync(
            allOrgMemberships, contactsTask.Result, _userManagerFactory);

        if (orgUsers.Count == 0)
        {
            return new ContactIdFilterResult { FilterRequired = true };
        }

        var membershipUserIds = allOrgMemberships
            .Where(m => m.Roles?.Any(r => roleIdsSet.Contains(r.RoleId) || roleIdsSet.Contains(r.RoleName)) == true)
            .Select(m => m.UserId)
            .ToHashSet();

        using var globalRoleManager = _roleManagerFactory();
        using var globalUserManager = _userManagerFactory();
        var qualifyingContactIds = (await GetContactIdsByGlobalRolesAsync(roleIds, orgUsers, globalRoleManager, globalUserManager))
            .ToHashSet();

        qualifyingContactIds.UnionWith(
            orgUsers
                .Where(u => membershipUserIds.Contains(u.Id))
                .Select(u => u.MemberId ?? u.Id)
                .Where(id => !string.IsNullOrEmpty(id)));

        return new ContactIdFilterResult { FilterRequired = true, Ids = qualifyingContactIds.ToList() };
    }

    private static async Task<IList<string>> GetContactIdsByGlobalRolesAsync(
        IList<string> roleIds,
        IList<ApplicationUser> orgUsers,
        RoleManager<Role> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        var roleIdsUpper = roleIds.Select(x => x.ToUpper()).ToList();
        var requestedRoleNames = await roleManager.Roles
            .Where(r => roleIdsUpper.Contains(r.Id.ToUpper()) || roleIdsUpper.Contains(r.Name.ToUpper()))
            .Select(r => r.Name)
            .ToListAsync();

        if (requestedRoleNames.Count == 0)
        {
            return [];
        }

        var requestedRoleNameSet = requestedRoleNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();

        foreach (var user in orgUsers)
        {
            var userRoleNames = await userManager.GetRolesAsync(user);
            if (userRoleNames.Any(requestedRoleNameSet.Contains))
            {
                var contactId = user.MemberId ?? user.Id;
                if (!string.IsNullOrEmpty(contactId))
                {
                    result.Add(contactId);
                }
            }
        }

        return result;
    }
}
