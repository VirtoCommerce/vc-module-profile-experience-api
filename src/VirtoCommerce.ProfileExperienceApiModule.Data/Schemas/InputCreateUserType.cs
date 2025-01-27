using GraphQL.Types;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputCreateUserType : InputObjectGraphType
    {
        public InputCreateUserType()
        {
            Field<NonNullGraphType<InputCreateApplicationUserType>>("applicationUser").Description("Application user to create");
        }
    }
}
