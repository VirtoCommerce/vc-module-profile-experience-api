using System;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Queries;

public class GetContactByIdQuery : GetMemberByIdQueryBase<ContactAggregate>
{
    public GetContactByIdQuery()
    {
    }

    [Obsolete("Use parameterless constructor with object initialization")]
    public GetContactByIdQuery(string contactId)
    {
        ContactId = contactId;
    }

    [Obsolete("Use Id instead")]
    public string ContactId
    {
        get => Id;
        set => Id = value;
    }
}
