using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Services;

public class MemberAddressService : IMemberAddressService
{
    private readonly IFavoriteAddressService _favoriteAddressService;
    private readonly IAddressSearchService _addressSearchService;

    public MemberAddressService(IFavoriteAddressService favoriteAddressService, IAddressSearchService addressSearchService)
    {
        _favoriteAddressService = favoriteAddressService;
        _addressSearchService = addressSearchService;
    }

    public virtual async Task<MemberAddressSearchResult> SearchMemberAddressesAsync(MemberAddressSearchCriteria criteria)
    {
        var searchCrtieria = GetAddressSearchCriteria(criteria);

        var addressesSearchResult = await _addressSearchService.SearchNoCloneAsync(searchCrtieria);

        var page = await ToMemberAddressesAsync(addressesSearchResult.Results, criteria.UserId);

        var result = new MemberAddressSearchResult()
        {
            TotalCount = addressesSearchResult.TotalCount,
            Results = page,
        };

        return result;
    }

    public virtual async Task<MemberAddress> ToMemberAddressAsync(Address address, string userId)
    {
        var favoriteAddressIds = await _favoriteAddressService.GetFavoriteAddressIdsAsync(userId);

        return ToMemberAddress(address, favoriteAddressIds);
    }

    public virtual async Task<IList<MemberAddress>> ToMemberAddressesAsync(IList<Address> addresses, string userId)
    {
        var favoriteAddressIds = await _favoriteAddressService.GetFavoriteAddressIdsAsync(userId);

        return addresses
            .Select(x => ToMemberAddress(x, favoriteAddressIds))
            .ToList();
    }

    protected virtual AddressSearchCriteria GetAddressSearchCriteria(MemberAddressSearchCriteria criteria)
    {
        var addressSearchCriteria = AbstractTypeFactory<AddressSearchCriteria>.TryCreateInstance();

        addressSearchCriteria.MemberId = criteria.MemberId;

        addressSearchCriteria.Take = criteria.Take;
        addressSearchCriteria.Skip = criteria.Skip;
        addressSearchCriteria.Sort = criteria.Sort;
        addressSearchCriteria.Keyword = criteria.Keyword;
        addressSearchCriteria.CountryCodes = criteria.CountryCodes;
        addressSearchCriteria.RegionIds = criteria.RegionIds;
        addressSearchCriteria.Cities = criteria.Cities;

        return addressSearchCriteria;
    }

    protected virtual MemberAddress ToMemberAddress(Address address, IList<string> favoriteAddressIds)
    {
        ArgumentNullException.ThrowIfNull(address);

        var result = AbstractTypeFactory<MemberAddress>.TryCreateInstance();

        result.Key = address.Key;
        result.IsDefault = address.IsDefault;
        result.City = address.City;
        result.CountryCode = address.CountryCode;
        result.CountryName = address.CountryName;
        result.Email = address.Email;
        result.FirstName = address.FirstName;
        result.MiddleName = address.MiddleName;
        result.LastName = address.LastName;
        result.Line1 = address.Line1;
        result.Line2 = address.Line2;
        result.Name = address.Name;
        result.Organization = address.Organization;
        result.Phone = address.Phone;
        result.PostalCode = address.PostalCode;
        result.RegionId = address.RegionId;
        result.RegionName = address.RegionName;
        result.Zip = address.Zip;
        result.OuterId = address.OuterId;
        result.Description = address.Description;
        result.AddressType = address.AddressType;

        if (favoriteAddressIds != null)
        {
            result.IsFavorite = favoriteAddressIds.Contains(address.Key);
        }

        return result;
    }
}
