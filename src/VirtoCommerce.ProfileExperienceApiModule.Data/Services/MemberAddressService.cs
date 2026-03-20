using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Model.Search;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models;
using VirtoCommerce.Xapi.Core.Models.Facets;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Services;

public class MemberAddressService : IMemberAddressService
{
    private readonly IFavoriteAddressService _favoriteAddressService;
    private readonly IAddressSearchService _addressSearchService;
    private readonly ICountriesService _countriesService;

    public MemberAddressService(IFavoriteAddressService favoriteAddressService, IAddressSearchService addressSearchService, ICountriesService countriesService)
    {
        _favoriteAddressService = favoriteAddressService;
        _addressSearchService = addressSearchService;
        _countriesService = countriesService;
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
            Facets = [],
        };

        // facets
        if (addressesSearchResult.Facets == null)
        {
            return result;
        }

        var countries = await _countriesService.GetCountriesAsync();

        if (!addressesSearchResult.Facets.Countries.IsNullOrEmpty())
        {
            var terms = addressesSearchResult.Facets.Countries
                .Select(x => new FacetTerm
                {
                    Term = x.Value,
                    Count = x.Count,
                    IsSelected = x.IsApplied,
                    Label = countries.FirstOrDefault(c => c.Id.EqualsIgnoreCase(x.CountryCode))?.Name ?? x.CountryCode,
                })
                .ToList();

            var termFacet = GetTermFacet("CountryCode", "Country", terms);
            result.Facets.Add(termFacet);
        }

        if (!addressesSearchResult.Facets.Cities.IsNullOrEmpty())
        {
            var terms = addressesSearchResult.Facets.Cities
                .Select(x => new FacetTerm
                {
                    Term = x.Value,
                    Count = x.Count,
                    IsSelected = x.IsApplied,
                    Label = x.Label,
                })
                .ToList();

            var termFacet = GetTermFacet("City", "City", terms);
            result.Facets.Add(termFacet);
        }

        if (!addressesSearchResult.Facets.Regions.IsNullOrEmpty())
        {
            List<FacetTerm> terms = [];
            Dictionary<string, IList<CountryRegion>> regionsDictionary = [];

            foreach (var regionFacet in addressesSearchResult.Facets.Regions)
            {
                var facetTerm = new FacetTerm
                {
                    Term = regionFacet.Value,
                    Count = regionFacet.Count,
                    IsSelected = regionFacet.IsApplied,
                };

                if (!regionsDictionary.TryGetValue(regionFacet.CountryCode, out var regions))
                {
                    regions = await _countriesService.GetCountryRegionsAsync(regionFacet.CountryCode);
                    regionsDictionary.Add(regionFacet.CountryCode, regions);
                }

                facetTerm.Label = regions.FirstOrDefault(x => x.Id.EqualsIgnoreCase(regionFacet.RegionId))?.Name ?? regionFacet.RegionId;
                terms.Add(facetTerm);
            }

            var termFacet = GetTermFacet("RegionId", "Region", terms);
            result.Facets.Add(termFacet);
        }

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
        addressSearchCriteria.UserId = criteria.UserId;
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

    private static TermFacetResult GetTermFacet(string name, string label, List<FacetTerm> terms)
    {
        return new TermFacetResult
        {
            Name = name,
            Label = label,
            Terms = terms
        };
    }
}
