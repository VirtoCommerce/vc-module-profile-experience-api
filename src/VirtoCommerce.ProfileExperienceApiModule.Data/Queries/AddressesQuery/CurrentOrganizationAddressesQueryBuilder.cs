using System.Threading.Tasks;
using GraphQL;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models;
using VirtoCommerce.ProfileExperienceApiModule.Data.Schemas;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;
using VirtoCommerce.Xapi.Core.BaseQueries;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries.AddressesQuery;

public class CurrentOrganizationAddressesQueryBuilder : SearchQueryBuilder<CurrentOrganizationAddressesQuery, MemberAddressSearchResult, MemberAddress, MemberAddressType>
{
    protected override string Name => "currentOrganizationAddresses";

    private readonly IProfileAuthorizationService _profileAuthorizationService;

    public CurrentOrganizationAddressesQueryBuilder(IMediator mediator, IAuthorizationService authorizationService, IProfileAuthorizationService profileAuthorizationService)
        : base(mediator, authorizationService)
    {
        _profileAuthorizationService = profileAuthorizationService;
    }

    protected override async Task BeforeMediatorSend(IResolveFieldContext<object> context, CurrentOrganizationAddressesQuery request)
    {
        await base.BeforeMediatorSend(context, request);
        await _profileAuthorizationService.CheckAuthAsync(context, request);
    }
}
