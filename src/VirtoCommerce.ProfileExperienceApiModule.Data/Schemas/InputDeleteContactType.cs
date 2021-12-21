using GraphQL.Types;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputDeleteContactType : InputObjectGraphType
    {
        public InputDeleteContactType()
        {
            Field<NonNullGraphType<StringGraphType>>("contactId");
        }
    }
}
