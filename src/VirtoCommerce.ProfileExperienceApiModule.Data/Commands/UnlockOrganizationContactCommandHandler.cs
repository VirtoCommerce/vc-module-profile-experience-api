using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class UnlockOrganizationContactCommandHandler : IRequestHandler<UnlockOrganizationContactCommand, ContactAggregate>
    {

        private readonly IContactAggregateRepository _contactAggregateRepository;

        public UnlockOrganizationContactCommandHandler(IContactAggregateRepository contactAggregateRepository)
        {
            _contactAggregateRepository = contactAggregateRepository;
        }

        public async Task<ContactAggregate> Handle(UnlockOrganizationContactCommand request, CancellationToken cancellationToken)
        {
            var contactAggregate = await _contactAggregateRepository.GetMemberAggregateRootByIdAsync<ContactAggregate>(request.UserId);


            contactAggregate.Contact.Status = "Approved";

            await _contactAggregateRepository.SaveAsync(contactAggregate);

            return contactAggregate;
        }
    }
}
