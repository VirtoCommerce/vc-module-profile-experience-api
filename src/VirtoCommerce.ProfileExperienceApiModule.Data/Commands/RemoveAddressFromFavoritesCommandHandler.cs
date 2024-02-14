using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.CustomerModule.Core.Services;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands;

public class RemoveAddressFromFavoritesCommandHandler : IRequestHandler<RemoveAddressFromFavoritesCommand, bool>
{
    private readonly IFavoriteAddressService _favoriteAddressService;

    public RemoveAddressFromFavoritesCommandHandler(IFavoriteAddressService favoriteAddressService)
    {
        _favoriteAddressService = favoriteAddressService;
    }

    public async Task<bool> Handle(RemoveAddressFromFavoritesCommand request, CancellationToken cancellationToken)
    {
        await _favoriteAddressService.RemoveAddressFromFavoritesAsync(request.UserId, request.AddressId);
        return true;
    }
}
