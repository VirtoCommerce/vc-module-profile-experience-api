using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.Platform.Core.Security;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Services;

/// <summary>
/// Resolves a user's global (account-level) ASP.NET Identity roles - the roles a user holds
/// directly, not tied to any OrganizationMembership or org-level role assignment. Shared between
/// ContactType.rolesInOrganization and the organization contact-roles/role-filter query handlers,
/// which all need to fold the same global roles into their respective per-member role sets.
/// </summary>
public static class GlobalRolesResolver
{
    public static async Task<IDictionary<string, IReadOnlyCollection<Role>>> GetGlobalRolesByUserAsync(
        IList<string> userIds,
        Func<RoleManager<Role>> roleManagerFactory,
        Func<UserManager<ApplicationUser>> userManagerFactory)
    {
        using var roleManager = roleManagerFactory();
        using var userManager = userManagerFactory();

        var rolesByName = roleManager.Roles.ToLookup(r => r.Name);

        var result = new Dictionary<string, IReadOnlyCollection<Role>>();

        foreach (var userId in userIds)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                continue;
            }

            var roleNames = await userManager.GetRolesAsync(user);
            if (roleNames.Count == 0)
            {
                continue;
            }

            result[userId] = roleNames
                .SelectMany(roleName => rolesByName[roleName])
                .Select(r => new Role { Id = r.Id, Name = r.Name })
                .ToList();
        }

        return result;
    }
}
