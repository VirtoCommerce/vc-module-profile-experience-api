using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputDeleteContactType : ExtendableInputObjectGraphType
    {
        public InputDeleteContactType()
        {
            Field<NonNullGraphType<StringGraphType>>("contactId");
        }
    }
}
