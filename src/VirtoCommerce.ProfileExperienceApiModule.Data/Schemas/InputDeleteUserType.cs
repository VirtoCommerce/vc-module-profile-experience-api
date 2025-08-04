using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputDeleteUserType : ExtendableInputObjectGraphType
    {
        public InputDeleteUserType()
        {
            Field<NonNullGraphType<ListGraphType<StringGraphType>>>("userNames");
        }
    }
}
