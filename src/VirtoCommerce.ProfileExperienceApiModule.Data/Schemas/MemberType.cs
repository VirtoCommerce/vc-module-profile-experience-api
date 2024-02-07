using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.ExperienceApiModule.Core.Services;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class MemberType : MemberBaseType<MemberAggregateRootBase>
    {
        public MemberType(IDynamicPropertyResolverService dynamicPropertyResolverService, IFavoriteAddressService favoriteAddressService)
            : base(dynamicPropertyResolverService, favoriteAddressService)
        {
        }
    }
}
