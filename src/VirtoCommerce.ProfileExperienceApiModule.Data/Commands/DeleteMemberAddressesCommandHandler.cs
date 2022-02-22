using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class DeleteMemberAddressesCommandHandler : IRequestHandler<DeleteMemberAddressesCommand, MemberAggregateRootBase>
    {
        private readonly IMemberAggregateRootRepository _memberAggregateRepository;

        public DeleteMemberAddressesCommandHandler(IMemberAggregateRootRepository memberAggregateRepository)
        {
            _memberAggregateRepository = memberAggregateRepository;
        }

        public virtual async Task<MemberAggregateRootBase> Handle(DeleteMemberAddressesCommand request, CancellationToken cancellationToken)
        {
            var memberAggregate = await _memberAggregateRepository.GetMemberAggregateRootByIdAsync<MemberAggregateRootBase>(request.MemberId);
            memberAggregate.DeleteAddresses(request.Addresses);
            await _memberAggregateRepository.SaveAsync(memberAggregate);

            return await _memberAggregateRepository.GetMemberAggregateRootByIdAsync<MemberAggregateRootBase>(request.MemberId);
        }
    }
}
