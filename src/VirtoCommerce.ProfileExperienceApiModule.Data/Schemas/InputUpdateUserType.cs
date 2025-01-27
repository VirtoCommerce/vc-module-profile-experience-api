using GraphQL.Types;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputUpdateUserType : InputObjectGraphType
    {
        public InputUpdateUserType()
        {
            Field<NonNullGraphType<InputUpdateApplicationUserType>>("applicationUser").Description("Application user to update");
        }
    }
}
