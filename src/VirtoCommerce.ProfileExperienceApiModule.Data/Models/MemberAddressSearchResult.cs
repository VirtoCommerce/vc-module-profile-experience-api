using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Xapi.Core.Models.Facets;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Models;

public class MemberAddressSearchResult : GenericSearchResult<MemberAddress>
{
    public IList<FacetResult> Facets { get; set; }
}
