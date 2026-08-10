using System.Collections.Generic;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries;

/// <summary>
/// Resolves a members-list "statuses" filter (effective status/lock state, e.g. Approved, Invited,
/// Locked) down to the set of matching contact ids for an organization.
/// </summary>
public class ResolveOrganizationStatusFilterQuery : IQuery<ContactIdFilterResult>
{
    public string OrganizationId { get; set; }

    public IList<string> Statuses { get; set; }
    public string StoreId { get; set; }
    public string CultureName { get; set; }
}
