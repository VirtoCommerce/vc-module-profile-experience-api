using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands;

public class MakeAddressDefaultCommandHandler : IRequestHandler<MakeAddressDefaultCommand, bool>
{
    private readonly IMemberAggregateRootRepository _memberAggregateRepository;

    public MakeAddressDefaultCommandHandler(IMemberAggregateRootRepository memberAggregateRepository)
    {
        _memberAggregateRepository = memberAggregateRepository;
    }

    public async Task<bool> Handle(MakeAddressDefaultCommand request, CancellationToken cancellationToken)
    {
        var memberAggregate = await _memberAggregateRepository.GetMemberAggregateRootByIdAsync<MemberAggregateRootBase>(request.MemberId);
        memberAggregate.MakeAddressDefault(request.AddressId);
        await _memberAggregateRepository.SaveAsync(memberAggregate);

        return true;
    }
}
