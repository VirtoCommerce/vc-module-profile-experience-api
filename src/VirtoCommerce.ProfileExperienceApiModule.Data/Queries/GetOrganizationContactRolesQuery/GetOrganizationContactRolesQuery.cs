using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries;

/// <summary>
/// Distinct roles currently assigned to at least one member of an organization - membership roles,
/// organization-level roles, and members' global (account-level) roles alike.
/// </summary>
public class GetOrganizationContactRolesQuery : IQuery<IList<Role>>
{
    public string OrganizationId { get; set; }
}
