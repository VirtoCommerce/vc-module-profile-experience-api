using GraphQL.Types;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputUpdateRoleType : ExtendableInputObjectGraphType
    {
        public InputUpdateRoleType()
        {
            Field<NonNullGraphType<InputUpdateRoleInnerType>>("role").Description("Role to update");
        }
    }
}
