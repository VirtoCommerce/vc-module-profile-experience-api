using VirtoCommerce.CustomerModule.Core.Services;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact
{
    public class ContactAggregateRepository : MemberAggregateRootRepository, IContactAggregateRepository
    {
        public ContactAggregateRepository(IMemberService memberService, IMemberAggregateFactory factory)
            : base(memberService, factory)
        {
        }
    }
}
