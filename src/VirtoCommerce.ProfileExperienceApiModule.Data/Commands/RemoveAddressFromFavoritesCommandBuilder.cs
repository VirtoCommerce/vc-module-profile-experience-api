using System.Threading.Tasks;
using GraphQL;
using GraphQL.Types;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.ExperienceApiModule.Core.BaseQueries;
using VirtoCommerce.ExperienceApiModule.Core.Extensions;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands;

public class RemoveAddressFromFavoritesCommandBuilder : CommandBuilder<RemoveAddressFromFavoritesCommand, bool, RemoveAddressFromFavoritesCommandType, BooleanGraphType>
{
    protected override string Name => "removeAddressFromFavorites";

    private readonly IProfileAuthorizationService _profileAuthorizationService;

    public RemoveAddressFromFavoritesCommandBuilder(
        IMediator mediator,
        IAuthorizationService authorizationService,
        IProfileAuthorizationService profileAuthorizationService)
        : base(mediator, authorizationService)
    {
        _profileAuthorizationService = profileAuthorizationService;
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
