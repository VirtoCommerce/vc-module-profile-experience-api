using GraphQL.Types;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputUpdateRoleType : InputObjectGraphType
    {
        public InputUpdateRoleType()
        {
            Field<NonNullGraphType<InputUpdateRoleInnerType>>("role").Description("Role to update");
        }
    }
}
