using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries.AddressesQuery;

public class CurrentOrganizationAddressesQueryHandler : BaseAddressesQueryHandler, IQueryHandler<CurrentOrganizationAddressesQuery, MemberAddressSearchResult>
{
    public CurrentOrganizationAddressesQueryHandler(IMemberAddressService memberAddressService)
        : base(memberAddressService)
    {
    }

    public virtual async Task<MemberAddressSearchResult> Handle(CurrentOrganizationAddressesQuery request, CancellationToken cancellationToken)
    {
        var memberId = await GetOrganizationIdAsync(request);
        return await SearchAddressesAsync(request, memberId);
    }

    protected virtual Task<string> GetOrganizationIdAsync(CurrentOrganizationAddressesQuery request)
    {
        return Task.FromResult(request.OrganizationId);
    }
}
