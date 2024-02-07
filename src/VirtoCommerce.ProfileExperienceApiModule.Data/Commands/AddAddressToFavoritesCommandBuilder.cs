using GraphQL;
using GraphQL.Types;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using VirtoCommerce.ExperienceApiModule.Core.BaseQueries;
using VirtoCommerce.ExperienceApiModule.Core.Extensions;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands;

public class AddAddressToFavoritesCommandBuilder : CommandBuilder<AddAddressToFavoritesCommand, bool, AddAddressToFavoritesCommandType, BooleanGraphType>
{
    protected override string Name => "addAddressToFavorites";

    public AddAddressToFavoritesCommandBuilder(IMediator mediator, IAuthorizationService authorizationService)
        : base(mediator, authorizationService)
    {
    }

    protected override AddAddressToFavoritesCommand GetRequest(IResolveFieldContext<object> context)
    {
        var request = base.GetRequest(context);
        request.UserId = context.GetCurrentUserId();
        return request;
    }
}
