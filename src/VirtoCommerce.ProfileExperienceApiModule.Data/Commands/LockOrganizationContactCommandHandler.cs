using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class LockOrganizationContactCommandHandler : IRequestHandler<LockOrganizationContactCommand, ContactAggregate>
    {
        private readonly IContactAggregateRepository _contactAggregateRepository;
        private readonly IAccountService _accountService;

        public LockOrganizationContactCommandHandler(IContactAggregateRepository contactAggregateRepository, IAccountService accountService)
        {
            _contactAggregateRepository = contactAggregateRepository;
            _accountService = accountService;
        }

        public virtual async Task<ContactAggregate> Handle(LockOrganizationContactCommand request, CancellationToken cancellationToken)
        {
            var contactAggregate = await _contactAggregateRepository.GetMemberAggregateRootByIdAsync<ContactAggregate>(request.UserId);

            contactAggregate.Contact.Status = ModuleConstants.ContactStatuses.Locked;

            await _contactAggregateRepository.SaveAsync(contactAggregate);

            var account = contactAggregate.Contact.SecurityAccounts?.FirstOrDefault();
            if (account != null)
            {
                await _accountService.LockAccountByIdAsync(account.Id);
            }

            return contactAggregate;
        }
    }
}
