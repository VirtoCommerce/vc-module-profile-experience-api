using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputUpdateRoleType : ExtendableInputGraphType
    {
        public InputUpdateRoleType()
        {
            Field<NonNullGraphType<InputUpdateRoleInnerType>>("role", description: "Role to update");
        }
    }
}
