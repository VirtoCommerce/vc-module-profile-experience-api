using System.Collections.Generic;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries;

/// <summary>
/// Result of resolving a members-list filter (role or status) down to the set of matching contact
/// ids. <see cref="FilterRequired"/> is false when the filter can be skipped entirely (e.g. every
/// requested role is already an organization-level role, satisfied by every member).
/// </summary>
public class ContactIdFilterResult
{
    public bool FilterRequired { get; set; }

    public IReadOnlyCollection<string> Ids { get; set; } = [];
}
