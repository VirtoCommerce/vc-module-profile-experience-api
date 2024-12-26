using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputUpdateUserType : ExtendableInputGraphType
    {
        public InputUpdateUserType()
        {
            Field<NonNullGraphType<InputUpdateApplicationUserType>>("applicationUser", description: "Application user to update");
        }
    }
}
