using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries.AddressesQuery;

public class CurrentCustomerAddressesQueryHandler : BaseAddressesQueryHandler, IQueryHandler<CurrentCustomerAddressesQuery, MemberAddressSearchResult>
{
    private readonly Func<UserManager<ApplicationUser>> _userManagerFactory;

    public CurrentCustomerAddressesQueryHandler(Func<UserManager<ApplicationUser>> userManagerFactory, IMemberAddressService memberAddressService)
        : base(memberAddressService)
    {
        _userManagerFactory = userManagerFactory;
    }

    public virtual async Task<MemberAddressSearchResult> Handle(CurrentCustomerAddressesQuery request, CancellationToken cancellationToken)
    {
        var memberId = await GetCustomerIdAsync(request);
        return await SearchAddressesAsync(request, memberId);
    }

    protected virtual async Task<string> GetCustomerIdAsync(CurrentCustomerAddressesQuery request)
    {
        using var userManager = _userManagerFactory();
        var user = await userManager.FindByIdAsync(request.UserId);
        return user?.MemberId;
    }
}
