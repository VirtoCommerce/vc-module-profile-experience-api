using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using VirtoCommerce.CustomerModule.Core.Extensions;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;
using VirtoCommerce.Xapi.Core.Services;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class UpdateContactCommandHandler(
        IContactAggregateRepository contactAggregateRepository,
        IDynamicPropertyUpdaterService dynamicPropertyUpdater,
        ICustomerPreferenceService customerPreferenceService,
        IMapper mapper)
        : IRequestHandler<UpdateContactCommand, ContactAggregate>
    {
        public virtual async Task<ContactAggregate> Handle(UpdateContactCommand request, CancellationToken cancellationToken)
        {
            var contactAggregate = await contactAggregateRepository.GetMemberAggregateRootByIdAsync<ContactAggregate>(request.Id);

            await UpdateContactAsync(contactAggregate.Contact, request);

            await contactAggregateRepository.SaveAsync(contactAggregate);

            return contactAggregate;
        }

        protected virtual async Task UpdateContactAsync(Contact contact, UpdateContactCommand request)
        {
            mapper.Map(request, contact);

            if (request.DynamicProperties != null)
            {
                await dynamicPropertyUpdater.UpdateDynamicPropertyValues(contact, request.DynamicProperties);
            }

            if (request.SelectedAddressId != null)
            {
                await customerPreferenceService.SaveSelectedAddressId(request.UserId, request.OrganizationId, request.SelectedAddressId);
            }
        }
    }
}
