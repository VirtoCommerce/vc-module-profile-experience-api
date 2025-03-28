using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.Xapi.Core.Services;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class MemberType : MemberBaseType<MemberAggregateRootBase>
    {
        public MemberType(
            IStoreService storeService,
            IDynamicPropertyResolverService dynamicPropertyResolverService,
            IMemberAddressService memberAddressService)
            : base(storeService, dynamicPropertyResolverService, memberAddressService)
        {
        }
    }
}
