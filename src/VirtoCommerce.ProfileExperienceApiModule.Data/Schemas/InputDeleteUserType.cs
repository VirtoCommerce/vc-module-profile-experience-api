using GraphQL.Types;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputDeleteUserType : InputObjectGraphType
    {
        public InputDeleteUserType()
        {
            Field<NonNullGraphType<ListGraphType<StringGraphType>>>("userNames");
        }
    }
}
