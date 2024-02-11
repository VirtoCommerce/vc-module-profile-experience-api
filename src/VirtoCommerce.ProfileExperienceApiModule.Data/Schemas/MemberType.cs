using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.ExperienceApiModule.Core.Services;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class MemberType : MemberBaseType<MemberAggregateRootBase>
    {
        public MemberType(
            IDynamicPropertyResolverService dynamicPropertyResolverService,
            IMemberAddressService memberAddressService,
            IFavoriteAddressService favoriteAddressService)
            : base(dynamicPropertyResolverService, memberAddressService, favoriteAddressService)
        {
        }
    }
}
