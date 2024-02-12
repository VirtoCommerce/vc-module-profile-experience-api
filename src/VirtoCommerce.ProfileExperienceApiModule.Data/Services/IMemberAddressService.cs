using System.Collections.Generic;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Services;

public interface IMemberAddressService
{
    public MemberAddress ToMemberAddress(Address address, IList<string> favoriteAddressIds);
}
