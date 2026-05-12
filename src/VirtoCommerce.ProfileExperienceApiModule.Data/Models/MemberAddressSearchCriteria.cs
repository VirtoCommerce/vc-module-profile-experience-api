using System.Collections.Generic;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Models;

public class MemberAddressSearchCriteria : SearchCriteriaBase
{
    public IList<string> Ids { get; set; }

    public string MemberId { get; set; }

    public string UserId { get; set; }

    public IList<string> CountryCodes { get; set; }

    public IList<string> RegionIds { get; set; }

    public IList<string> Cities { get; set; }
}
