using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.CustomerModule.Core.Services;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands;

public class AddAddressToFavoritesCommandHandler : IRequestHandler<AddAddressToFavoritesCommand, bool>
{
    private readonly IFavoriteAddressService _favoriteAddressService;

    public AddAddressToFavoritesCommandHandler(IFavoriteAddressService favoriteAddressService)
    {
        _favoriteAddressService = favoriteAddressService;
    }

    public async Task<bool> Handle(AddAddressToFavoritesCommand request, CancellationToken cancellationToken)
    {
        await _favoriteAddressService.AddAddressToFavoritesAsync(request.UserId, request.AddressId);
        return true;
    }
}
