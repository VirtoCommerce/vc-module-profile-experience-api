using GraphQL.Types;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputPersonalDataType : ExtendableInputObjectGraphType<PersonalData>
    {
        public InputPersonalDataType()
        {
            Field<StringGraphType>(nameof(ApplicationUser.Email));
            Field<StringGraphType>(nameof(Contact.FullName));
            Field<StringGraphType>(nameof(Contact.FirstName));
            Field<StringGraphType>(nameof(Contact.LastName));
            Field<StringGraphType>(nameof(Contact.MiddleName));
        }
    }
}
