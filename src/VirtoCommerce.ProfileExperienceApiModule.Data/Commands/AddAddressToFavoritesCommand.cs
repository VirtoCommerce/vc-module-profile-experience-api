using GraphQL.Types;
using VirtoCommerce.ExperienceApiModule.Core.Infrastructure;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands;

public class AddAddressToFavoritesCommand : ICommand<bool>
{
    public string UserId { get; set; }
    public string AddressId { get; set; }
}

public class AddAddressToFavoritesCommandType : InputObjectGraphType<AddAddressToFavoritesCommand>
{
    public AddAddressToFavoritesCommandType()
    {
        Field(x => x.AddressId);
    }
}
