using VirtoCommerce.ExperienceApiModule.Core.Services;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;

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
