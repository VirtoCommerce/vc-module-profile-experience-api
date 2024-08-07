using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;

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

            _mapper.Map(request, contactAggregate.Contact);

            if (request.DynamicProperties != null)
            {
                await _dynamicPropertyUpdater.UpdateDynamicPropertyValues(contactAggregate.Contact, request.DynamicProperties);
            }

            await _contactAggregateRepository.SaveAsync(contactAggregate);

            return contactAggregate;
        }
    }
}
