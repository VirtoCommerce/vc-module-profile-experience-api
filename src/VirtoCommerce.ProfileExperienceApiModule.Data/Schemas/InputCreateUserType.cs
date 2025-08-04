using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputCreateUserType : ExtendableInputObjectGraphType
    {
        public InputCreateUserType()
        {
            Field<NonNullGraphType<InputCreateApplicationUserType>>("applicationUser").Description("Application user to create");
        }
    }
}
