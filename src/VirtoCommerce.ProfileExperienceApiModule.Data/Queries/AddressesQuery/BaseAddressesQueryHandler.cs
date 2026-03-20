using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries.AddressesQuery;

public class CurrentCustomerAddressesQueryHandler
{

}

public class CurrentOrganizationAddressesQueryHandler
{

}

public class BaseAddressesQueryHandler : IQueryHandler<CurrentCustomerAddressesQuery, MemberAddressSearchResult>, IQueryHandler<CurrentOrganizationAddressesQuery, MemberAddressSearchResult>
{
    private readonly IMemberAddressService _memberAddressService;
    private readonly Func<UserManager<ApplicationUser>> _userManagerFactory;

    public BaseAddressesQueryHandler(Func<UserManager<ApplicationUser>> userManagerFactory, IMemberAddressService memberAddressService)
    {
        _userManagerFactory = userManagerFactory;
        _memberAddressService = memberAddressService;
    }

    public virtual async Task<MemberAddressSearchResult> Handle(CurrentCustomerAddressesQuery request, CancellationToken cancellationToken)
    {
        var memberId = await GetCustomerIdAsync(request);
        return await SearchAddressesAsync(request, memberId);
    }

    public virtual async Task<MemberAddressSearchResult> Handle(CurrentOrganizationAddressesQuery request, CancellationToken cancellationToken)
    {
        var memberId = await GetOrganizationIdAsync(request);
        return await SearchAddressesAsync(request, memberId);
    }

    protected virtual async Task<string> GetCustomerIdAsync(CurrentCustomerAddressesQuery request)
    {
        using var userManager = _userManagerFactory();
        var user = await userManager.FindByIdAsync(request.UserId);
        return user?.MemberId;
    }

    protected virtual Task<string> GetOrganizationIdAsync(CurrentOrganizationAddressesQuery request)
    {
        return Task.FromResult(request.OrganizationId);
    }

    private async Task<MemberAddressSearchResult> SearchAddressesAsync(BaseAddressesQuery request, string memberId)
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
