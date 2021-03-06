using System.Threading;
using System.Threading.Tasks;
using VirtoCommerce.ExperienceApiModule.Core.Infrastructure;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries
{
    public class GetContactByIdQueryHandler : IQueryHandler<GetContactByIdQuery, ContactAggregate>
    {
        private readonly IContactAggregateRepository _contactAggregateRepository;

        public GetContactByIdQueryHandler(IContactAggregateRepository contactAggregateRepository)
        {
            _contactAggregateRepository = contactAggregateRepository;
        }

        public virtual Task<ContactAggregate> Handle(GetContactByIdQuery request, CancellationToken cancellationToken)
        {
            return _contactAggregateRepository.GetMemberAggregateRootByIdAsync<ContactAggregate>(request.ContactId);
        }
    }
}
