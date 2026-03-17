using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates;
using VirtoCommerce.ProfileExperienceApiModule.Data.Validators;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class UpdateMemberAddressesCommandHandler : IRequestHandler<UpdateMemberAddressesCommand, MemberAggregateRootBase>
    {
        private readonly IMemberAggregateRootRepository _memberAggregateRepository;
        private readonly AddressValidator _addressValidator;

        public UpdateMemberAddressesCommandHandler(
            IMemberAggregateRootRepository memberAggregateRepository,
            AddressValidator addressValidator)
        {
            _memberAggregateRepository = memberAggregateRepository;
            _addressValidator = addressValidator;
        }

        public virtual async Task<MemberAggregateRootBase> Handle(UpdateMemberAddressesCommand request, CancellationToken cancellationToken)
        {
            if (request.Addresses != null)
            {
                foreach (var address in request.Addresses)
                {
                    await _addressValidator.ValidateAndThrowAsync(address, cancellationToken);
                }
            }

            var memberAggregate = await _memberAggregateRepository.GetMemberAggregateRootByIdAsync<MemberAggregateRootBase>(request.MemberId);
            memberAggregate.UpdateAddresses(request.Addresses);
            await _memberAggregateRepository.SaveAsync(memberAggregate);

            return await _memberAggregateRepository.GetMemberAggregateRootByIdAsync<MemberAggregateRootBase>(request.MemberId);
        }
    }
}
