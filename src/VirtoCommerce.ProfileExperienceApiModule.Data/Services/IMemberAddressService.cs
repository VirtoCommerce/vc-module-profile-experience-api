using System.Collections.Generic;
using System.Threading.Tasks;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Services;

public interface IMemberAddressService
{
    Task<MemberAddress> ToMemberAddressAsync(Address address, string userId);
    Task<IList<MemberAddress>> ToMemberAddressesAsync(IList<Address> addresses, string userId);
}
