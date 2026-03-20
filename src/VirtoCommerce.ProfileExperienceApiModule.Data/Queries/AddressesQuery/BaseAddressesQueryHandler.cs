using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries.AddressesQuery;

public abstract class BaseAddressesQueryHandler
{
    private readonly IMemberAddressService _memberAddressService;

    protected BaseAddressesQueryHandler(IMemberAddressService memberAddressService)
    {
        _memberAddressService = memberAddressService;
    }

    protected virtual async Task<MemberAddressSearchResult> SearchAddressesAsync(BaseAddressesQuery request, string memberId)
    {
        if (memberId.IsNullOrEmpty())
        {
            return new MemberAddressSearchResult();
        }

        var criteria = GetAddressSearchCriteria(request, memberId);
        return await _memberAddressService.SearchMemberAddressesAsync(criteria);
    }

    protected virtual MemberAddressSearchCriteria GetAddressSearchCriteria(BaseAddressesQuery criteria, string memberId)
    {
        var addressSearchCriteria = AbstractTypeFactory<MemberAddressSearchCriteria>.TryCreateInstance();

        addressSearchCriteria.UserId = criteria.UserId;
        addressSearchCriteria.MemberId = memberId;

        addressSearchCriteria.Take = criteria.Take;
        addressSearchCriteria.Skip = criteria.Skip;
        addressSearchCriteria.Sort = criteria.Sort;
        addressSearchCriteria.Keyword = criteria.Keyword;
        addressSearchCriteria.CountryCodes = criteria.CountryCodes;
        addressSearchCriteria.RegionIds = criteria.RegionIds;
        addressSearchCriteria.Cities = criteria.Cities;

        return addressSearchCriteria;
    }
}
