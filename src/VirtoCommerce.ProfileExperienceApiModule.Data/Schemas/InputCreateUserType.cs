using GraphQL.Types;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputCreateUserType : InputObjectGraphType
    {
        public InputCreateUserType()
        {
            Field<NonNullGraphType<InputCreateApplicationUserType>>("applicationUser", description: "Application user to create");
        }
    }
}
