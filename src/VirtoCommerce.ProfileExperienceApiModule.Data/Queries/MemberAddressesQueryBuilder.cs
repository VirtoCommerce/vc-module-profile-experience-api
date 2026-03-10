using System.Threading.Tasks;
using GraphQL;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models;
using VirtoCommerce.ProfileExperienceApiModule.Data.Schemas;
using VirtoCommerce.Xapi.Core.BaseQueries;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries;

public class MemberAddressesQueryBuilder : SearchQueryBuilder<MemberAddressesQuery, MemberAddressSearchResult, MemberAddress, MemberAddressType>
{
    protected override string Name => "memberAddresses";

    public MemberAddressesQueryBuilder(IMediator mediator, IAuthorizationService authorizationService)
        : base(mediator, authorizationService)
    {
    }

    protected override Task BeforeMediatorSend(IResolveFieldContext<object> context, MemberAddressesQuery request)
    {
        return base.BeforeMediatorSend(context, request);
    }
}
