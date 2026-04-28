using MediatR;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries.AddressesQuery;

public class CurrentOrganizationAddressesQueryBuilder : BaseAddressesQueryBuilder<CurrentOrganizationAddressesQuery>
{
    protected override string Name => "currentOrganizationAddresses";

    public CurrentOrganizationAddressesQueryBuilder(IMediator mediator, IAuthorizationService authorizationService, IProfileAuthorizationService profileAuthorizationService)
        : base(mediator, authorizationService, profileAuthorizationService)
    {
    }
}
