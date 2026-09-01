using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.Platform.Core.Security;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Services;

/// <summary>
/// Resolves the ApplicationUser accounts that belong to an organization - the union of users with
/// an OrganizationMembership row and users whose account is directly linked to one of the org's
/// contacts. Shared between GetOrganizationContactRolesQueryHandler and
/// ResolveOrganizationRoleFilterQueryHandler, which both need this same set before resolving roles.
/// </summary>
public static class OrganizationUsersResolver
{
    public static async Task<List<ApplicationUser>> GetOrganizationUsersAsync(
        IList<OrganizationMembership> memberships,
        IList<Member> contacts,
        Func<UserManager<ApplicationUser>> userManagerFactory)
    {
        var orgMembershipUserIds = memberships.Select(m => m.UserId).ToHashSet();
        var orgContactIds = contacts.Select(c => c.Id).ToHashSet();

        using var userManager = userManagerFactory();

        return await userManager.Users
            .Where(u => orgMembershipUserIds.Contains(u.Id) ||
                        (!string.IsNullOrEmpty(u.MemberId) && orgContactIds.Contains(u.MemberId)))
            .ToListAsync();
    }
}
