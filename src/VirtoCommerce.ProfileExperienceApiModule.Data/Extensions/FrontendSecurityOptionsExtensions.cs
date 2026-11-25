using System;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Configuration;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Extensions;

public static class FrontendSecurityOptionsExtensions
{
    public static bool IsRoleAllowed(this FrontendSecurityOptions options, Role role)
    {
        var whitelist = options.MembershipRolesWhitelist ?? ModuleConstants.Security.DefaultMembershipRolesWhitelist;

        return whitelist.Contains(role.Id, StringComparer.OrdinalIgnoreCase) ||
               whitelist.Contains(role.Name, StringComparer.OrdinalIgnoreCase);
    }
}
