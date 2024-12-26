using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputCreateUserType : ExtendableInputGraphType
    {
        public InputCreateUserType()
        {
            Field<NonNullGraphType<InputCreateApplicationUserType>>("applicationUser", description: "Application user to create");
        }
    }
}
