using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands;

public class AddAddressToFavoritesCommand : ICommand<bool>
{
    public string UserId { get; set; }
    public string AddressId { get; set; }
}

public class AddAddressToFavoritesCommandType : ExtendableInputObjectGraphType<AddAddressToFavoritesCommand>
{
    public AddAddressToFavoritesCommandType()
    {
        Field(x => x.AddressId);
    }
}
