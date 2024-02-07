using GraphQL;
using GraphQL.Types;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.ExperienceApiModule.Core.BaseQueries;
using VirtoCommerce.ExperienceApiModule.Core.Extensions;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands;

public class RemoveAddressFromFavoritesCommandBuilder : CommandBuilder<RemoveAddressFromFavoritesCommand, bool, RemoveAddressFromFavoritesCommandType, BooleanGraphType>
{
    protected override string Name => "removeAddressFromFavorites";

    public RemoveAddressFromFavoritesCommandBuilder(IMediator mediator, IAuthorizationService authorizationService)
        : base(mediator, authorizationService)
    {
    }

    protected override RemoveAddressFromFavoritesCommand GetRequest(IResolveFieldContext<object> context)
    {
        var request = base.GetRequest(context);
        request.UserId = context.GetCurrentUserId();
        return request;
    }
}
