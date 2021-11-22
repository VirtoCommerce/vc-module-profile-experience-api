using VirtoCommerce.ExperienceApiModule.Core.Infrastructure;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries
{
    public class GetContactByIdQuery : IQuery<ContactAggregate>
    {
        public GetContactByIdQuery(string contactId)
        {
            ContactId = contactId;
        }

        public string ContactId { get; set; }
    }
}
