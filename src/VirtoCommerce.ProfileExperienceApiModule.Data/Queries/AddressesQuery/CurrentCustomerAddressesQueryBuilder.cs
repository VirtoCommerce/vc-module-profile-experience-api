using MediatR;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries.AddressesQuery;

public class CurrentCustomerAddressesQueryBuilder : BaseAddressesQueryBuilder<CurrentCustomerAddressesQuery>
{
    protected override string Name => "currentCustomerAddresses";

    public CurrentCustomerAddressesQueryBuilder(IMediator mediator, IAuthorizationService authorizationService, IProfileAuthorizationService profileAuthorizationService)
        : base(mediator, authorizationService, profileAuthorizationService)
    {
    }
}
