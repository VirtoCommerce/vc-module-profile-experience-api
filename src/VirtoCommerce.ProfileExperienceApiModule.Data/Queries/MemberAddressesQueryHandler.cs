using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries;

public class MemberAddressesQueryHandler : IQueryHandler<MemberAddressesQuery, MemberAddressSearchResult>
{
    private readonly IMemberAddressService _memberAddressService;
    private readonly Func<UserManager<ApplicationUser>> _userManagerFactory;

    public MemberAddressesQueryHandler(Func<UserManager<ApplicationUser>> userManagerFactory, IMemberAddressService memberAddressService)
    {
        _userManagerFactory = userManagerFactory;
        _memberAddressService = memberAddressService;
    }

    public async Task<MemberAddressSearchResult> Handle(MemberAddressesQuery request, CancellationToken cancellationToken)
    {
        var result = new MemberAddressSearchResult();

        var memberId = request.MemberId;

        if (memberId.IsNullOrEmpty())
        {
            using var userManager = _userManagerFactory();
            var user = await userManager.FindByIdAsync(request.UserId);
            memberId = user?.MemberId;
        }

        if (!memberId.IsNullOrEmpty())
        {
            var criteria = GetAddressSearchCriteria(request, memberId);
            result = await _memberAddressService.SearchMemberAddressesAsync(criteria);
        }

        return result;
    }

    protected virtual MemberAddressSearchCriteria GetAddressSearchCriteria(MemberAddressesQuery criteria, string memberId)
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
