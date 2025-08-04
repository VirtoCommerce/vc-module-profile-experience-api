using GraphQL.Types;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputLockUnlockOrganizationContactType : InputObjectGraphType
    {
        public InputLockUnlockOrganizationContactType()
        {
            Field<StringGraphType>("UserId");
        }
    }
}
