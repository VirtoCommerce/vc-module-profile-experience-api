using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;
using VirtoCommerce.Xapi.Core.Services;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class UpdateContactCommandHandler : IRequestHandler<UpdateContactCommand, ContactAggregate>
    {
        private readonly IContactAggregateRepository _contactAggregateRepository;
        private readonly IDynamicPropertyUpdaterService _dynamicPropertyUpdater;
        private readonly IMapper _mapper;

        public UpdateContactCommandHandler(IContactAggregateRepository contactAggregateRepository,
            IDynamicPropertyUpdaterService dynamicPropertyUpdater,
            IMapper mapper)
        {
            _contactAggregateRepository = contactAggregateRepository;
            _dynamicPropertyUpdater = dynamicPropertyUpdater;
            _mapper = mapper;
        }

        public virtual async Task<ContactAggregate> Handle(UpdateContactCommand request, CancellationToken cancellationToken)
        {
            var contactAggregate = await _contactAggregateRepository.GetMemberAggregateRootByIdAsync<ContactAggregate>(request.Id);

            UpdateContact(request, contactAggregate.Contact);

            if (request.DynamicProperties != null)
            {
                await _dynamicPropertyUpdater.UpdateDynamicPropertyValues(contactAggregate.Contact, request.DynamicProperties);
            }

            await _contactAggregateRepository.SaveAsync(contactAggregate);

            return contactAggregate;
        }

        protected virtual void UpdateContact(UpdateContactCommand request, Contact contact)
        {
            _mapper.Map(request, contact);
        }
    }
}
