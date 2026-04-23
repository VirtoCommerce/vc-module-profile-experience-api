using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries
{
    public class CheckDuplicateAddressQueryHandler : IQueryHandler<CheckDuplicateAddressQuery, CheckDuplicateAddressResult>
    {
        private readonly IMemberAggregateRootRepository _aggregateRepository;

        public CheckDuplicateAddressQueryHandler(IMemberAggregateRootRepository aggregateRepository)
        {
            _aggregateRepository = aggregateRepository;
        }

        public virtual async Task<CheckDuplicateAddressResult> Handle(CheckDuplicateAddressQuery request, CancellationToken cancellationToken)
        {
            var aggregate = await _aggregateRepository.GetMemberAggregateRootByIdAsync<MemberAggregateRootBase>(request.MemberId);

            return new CheckDuplicateAddressResult
            {
                IsDuplicated = aggregate?.IsDuplicateAddress(request.Address) ?? false,
            };
        }
    }
}
