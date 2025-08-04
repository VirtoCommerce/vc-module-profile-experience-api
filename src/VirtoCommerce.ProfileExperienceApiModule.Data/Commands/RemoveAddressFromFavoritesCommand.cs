using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands;

public class RemoveAddressFromFavoritesCommand : ICommand<bool>
{
    public string UserId { get; set; }
    public string AddressId { get; set; }
}

public class RemoveAddressFromFavoritesCommandType : ExtendableInputObjectGraphType<RemoveAddressFromFavoritesCommand>
{
    public RemoveAddressFromFavoritesCommandType()
    {
        Field(x => x.AddressId);
    }
}
