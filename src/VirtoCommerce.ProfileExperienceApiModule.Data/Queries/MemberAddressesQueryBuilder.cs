using System.Threading.Tasks;
using GraphQL;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models;
using VirtoCommerce.ProfileExperienceApiModule.Data.Schemas;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;
using VirtoCommerce.Xapi.Core.BaseQueries;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries;

public class MemberAddressesQueryBuilder : SearchQueryBuilder<MemberAddressesQuery, MemberAddressSearchResult, MemberAddress, MemberAddressType>
{
    protected override string Name => "memberAddresses";

    private readonly IProfileAuthorizationService _profileAuthorizationService;

    public MemberAddressesQueryBuilder(IMediator mediator, IAuthorizationService authorizationService, IProfileAuthorizationService profileAuthorizationService)
        : base(mediator, authorizationService)
    {
        _profileAuthorizationService = profileAuthorizationService;
    }

    protected override async Task BeforeMediatorSend(IResolveFieldContext<object> context, MemberAddressesQuery request)
    {
        await base.BeforeMediatorSend(context, request);
        await _profileAuthorizationService.CheckAuthAsync(context, request);
    }
}
