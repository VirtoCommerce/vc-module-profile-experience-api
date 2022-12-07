using VirtoCommerce.CustomerModule.Core.Services;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Vendor;

public class VendorAggregateRepository: MemberAggregateRootRepository, IVendorAggregateRootRepository
{
    public VendorAggregateRepository(IMemberService memberService, IMemberAggregateFactory factory) : base(memberService, factory)
    {
    }
}
