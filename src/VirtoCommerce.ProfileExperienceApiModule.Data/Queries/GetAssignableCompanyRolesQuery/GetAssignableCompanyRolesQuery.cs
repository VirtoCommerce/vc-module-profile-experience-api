using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries;

/// <summary>
/// Real platform roles a company member can be assigned within a store - the intersection of the
/// store's role whitelist and the roles that actually exist on the platform.
/// </summary>
public class GetAssignableCompanyRolesQuery : IQuery<IList<Role>>
{
    public string StoreId { get; set; }
    public string CultureName { get; set; }
}
