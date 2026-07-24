using System;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Types;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Extensions;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands;

public class RemoveAddressFromFavoritesCommandBuilder : CommandBuilder<RemoveAddressFromFavoritesCommand, bool, RemoveAddressFromFavoritesCommandType, BooleanGraphType>
{
    protected override string Name => "removeAddressFromFavorites";

    private readonly IProfileAuthorizationService _profileAuthorizationService;

    public RemoveAddressFromFavoritesCommandBuilder(
        IAuthorizationService authorizationService,
        IProfileAuthorizationService profileAuthorizationService)
        : base(authorizationService)
    {
        _profileAuthorizationService = profileAuthorizationService;
    }

    [Obsolete("Use the constructor without IMediator. The mediator is resolved from context.RequestServices per request.", DiagnosticId = "VC0015", UrlFormat = "https://docs.virtocommerce.org/products/products-virto3-versions")]
    public RemoveAddressFromFavoritesCommandBuilder(IMediator mediator, IAuthorizationService authorizationService, IProfileAuthorizationService profileAuthorizationService)
        : this(authorizationService, profileAuthorizationService)
    {
    }

    protected override RemoveAddressFromFavoritesCommand GetRequest(IResolveFieldContext<object> context)
    {
        var request = base.GetRequest(context);
        request.UserId = context.GetCurrentUserId();
        return request;
    }

    protected override async Task BeforeMediatorSend(IResolveFieldContext<object> context, RemoveAddressFromFavoritesCommand request)
    {
        await base.BeforeMediatorSend(context, request);
        await _profileAuthorizationService.CheckAuthAsync(context, request);
    }
}
