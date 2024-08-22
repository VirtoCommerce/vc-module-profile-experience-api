using System.Threading.Tasks;
using GraphQL;
using GraphQL.Types;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Extensions;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands;

public class MakeAddressDefaultCommandBuilder : CommandBuilder<MakeAddressDefaultCommand, bool, MakeAddressDefaultCommandType, BooleanGraphType>
{
    protected override string Name => "makeAddressDefault";

    private readonly IProfileAuthorizationService _profileAuthorizationService;

    public MakeAddressDefaultCommandBuilder(
        IMediator mediator,
        IAuthorizationService authorizationService,
        IProfileAuthorizationService profileAuthorizationService)
        : base(mediator, authorizationService)
    {
        _profileAuthorizationService = profileAuthorizationService;
    }

    protected override MakeAddressDefaultCommand GetRequest(IResolveFieldContext<object> context)
    {
        var request = base.GetRequest(context);
        request.UserId = context.GetCurrentUserId();
        return request;
    }

    protected override async Task BeforeMediatorSend(IResolveFieldContext<object> context, MakeAddressDefaultCommand request)
    {
        await base.BeforeMediatorSend(context, request);
        await _profileAuthorizationService.CheckAuthAsync(context, request);
    }
}
