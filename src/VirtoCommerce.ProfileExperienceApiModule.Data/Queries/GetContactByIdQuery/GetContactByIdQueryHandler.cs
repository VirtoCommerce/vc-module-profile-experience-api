using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries;

public class GetContactByIdQueryHandler : GetMemberByIdQueryHandlerBase<GetContactByIdQuery, ContactAggregate>
{
    public GetContactByIdQueryHandler(IContactAggregateRepository contactAggregateRepository): base(contactAggregateRepository)
    {
    }
}
