using System;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries.AddressesQuery;

public class CurrentCustomerAddressesQueryBuilder : BaseAddressesQueryBuilder<CurrentCustomerAddressesQuery>
{
    protected override string Name => "currentCustomerAddresses";

    public CurrentCustomerAddressesQueryBuilder(IAuthorizationService authorizationService, IProfileAuthorizationService profileAuthorizationService)
        : base(authorizationService, profileAuthorizationService)
    {
    }

    [Obsolete("Use the constructor without IMediator. The mediator is resolved from context.RequestServices per request.", DiagnosticId = "VC0015", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
    public CurrentCustomerAddressesQueryBuilder(IMediator mediator, IAuthorizationService authorizationService, IProfileAuthorizationService profileAuthorizationService)
        : this(authorizationService, profileAuthorizationService)
    {
    }
}
