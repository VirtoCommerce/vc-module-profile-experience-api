using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class RemoveMemberFromOrganizationCommandHandler : IRequestHandler<RemoveMemberFromOrganizationCommand, ContactAggregate>
    {
        private readonly IContactAggregateRepository _contactAggregateRepository;

        public RemoveMemberFromOrganizationCommandHandler(
            IContactAggregateRepository contactAggregateRepository
            )
        {
            _contactAggregateRepository = contactAggregateRepository;
        }

        public async Task<ContactAggregate> Handle(RemoveMemberFromOrganizationCommand request, CancellationToken cancellationToken)
        {
            var contactAggregate = await _contactAggregateRepository.GetMemberAggregateRootByIdAsync<ContactAggregate>(request.ContactId);
            contactAggregate.Contact.Organizations.Remove(request.OrganizationId);
            await _contactAggregateRepository.SaveAsync(contactAggregate);

            return contactAggregate;
        }
    }
}
