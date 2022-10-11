using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class LockOrganizationContactCommandHandler : IRequestHandler<LockOrganizationContactCommand, ContactAggregate>
    {
        private readonly IContactAggregateRepository _contactAggregateRepository;

        public LockOrganizationContactCommandHandler(IContactAggregateRepository contactAggregateRepository)
        {
            _contactAggregateRepository = contactAggregateRepository;
        }

        public async Task<ContactAggregate> Handle(LockOrganizationContactCommand request, CancellationToken cancellationToken)
        {
            var contactAggregate = await _contactAggregateRepository.GetMemberAggregateRootByIdAsync<ContactAggregate>(request.UserId);

            contactAggregate.Contact.Status = ModuleConstants.ContactStatuses.Locked;

            await _contactAggregateRepository.SaveAsync(contactAggregate);

            return contactAggregate;
        }
    }
}
