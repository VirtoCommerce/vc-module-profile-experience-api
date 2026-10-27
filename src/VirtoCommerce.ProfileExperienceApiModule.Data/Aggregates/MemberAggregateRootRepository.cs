using System.Threading.Tasks;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core.Common;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates
{
    public class MemberAggregateRootRepository : IMemberAggregateRootRepository
    {
        protected readonly IMemberService _memberService;
        protected readonly IMemberAggregateFactory _memberAggregateFactory;

        public MemberAggregateRootRepository(IMemberService memberService, IMemberAggregateFactory factory)
        {
            _memberService = memberService;
            _memberAggregateFactory = factory;
        }


        public async Task<T> GetMemberAggregateRootByIdAsync<T>(string id) where T : class, IMemberAggregateRoot
        {
            var member = await _memberService.GetByIdAsync(id);

            // The member service returns its cached instance; commands change the aggregate before validating and
            // saving it, so a rejected save would otherwise leave the rejected values in the cache
            return _memberAggregateFactory.Create<T>(member?.CloneTyped());
        }

        public Task SaveAsync(IMemberAggregateRoot aggregate)
        {
            return _memberService.SaveChangesAsync(new[] { aggregate.Member });
        }

        public Task DeleteAsync(string id)
        {
            return _memberService.DeleteAsync(new[] { id });
        }
    }
}
