using System.Collections.Generic;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries;

/// <summary>
/// Resolves a members-list "roleIds" filter (org-level, membership, or global role ids/names) down
/// to the set of matching contact ids for an organization.
/// </summary>
public class ResolveOrganizationRoleFilterQuery : IQuery<ContactIdFilterResult>
{
    public string OrganizationId { get; set; }

    public IList<string> RoleIds { get; set; }
}
