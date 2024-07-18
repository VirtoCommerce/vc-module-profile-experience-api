using System.Threading.Tasks;
using GraphQL;
using GraphQL.Types;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.Xapi.Core.BaseQueries;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands;

public class AddAddressToFavoritesCommandBuilder : CommandBuilder<AddAddressToFavoritesCommand, bool, AddAddressToFavoritesCommandType, BooleanGraphType>
{
    protected override string Name => "addAddressToFavorites";

    private readonly IProfileAuthorizationService _profileAuthorizationService;

    public AddAddressToFavoritesCommandBuilder(
        IMediator mediator,
        IAuthorizationService authorizationService,
        IProfileAuthorizationService profileAuthorizationService)
        : base(mediator, authorizationService)
    {
        _profileAuthorizationService = profileAuthorizationService;
    }

    protected override AddAddressToFavoritesCommand GetRequest(IResolveFieldContext<object> context)
    {
        var request = base.GetRequest(context);
        request.UserId = context.GetCurrentUserId();
        return request;
    }

    protected override async Task BeforeMediatorSend(IResolveFieldContext<object> context, AddAddressToFavoritesCommand request)
    {
        await base.BeforeMediatorSend(context, request);
        await _profileAuthorizationService.CheckAuthAsync(context, request);
    }
}
