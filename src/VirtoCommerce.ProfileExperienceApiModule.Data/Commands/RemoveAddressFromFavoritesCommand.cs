using GraphQL.Types;
using VirtoCommerce.ExperienceApiModule.Core.Infrastructure;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands;

public class RemoveAddressFromFavoritesCommand : ICommand<bool>
{
    public string UserId { get; set; }
    public string AddressId { get; set; }
}

public class RemoveAddressFromFavoritesCommandType : InputObjectGraphType<RemoveAddressFromFavoritesCommand>
{
    public RemoveAddressFromFavoritesCommandType()
    {
        Field(x => x.AddressId);
    }
}
