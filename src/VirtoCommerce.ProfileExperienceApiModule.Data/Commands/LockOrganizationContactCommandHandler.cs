using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using VirtoCommerce.ExperienceApiModule.Core.Services;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;
using VirtoCommerce.ProfileExperienceApiModule.Data.Queries;

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

            contactAggregate.Contact.Status = "Locked";

            await _contactAggregateRepository.SaveAsync(contactAggregate);

            return contactAggregate;
        }
    }
}
