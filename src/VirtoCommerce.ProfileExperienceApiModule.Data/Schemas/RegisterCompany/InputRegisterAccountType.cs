using GraphQL.Types;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas.RegisterCompany
{
    public class InputRegisterAccountType : InputObjectGraphType
    {
        public InputRegisterAccountType()
        {
            Field<NonNullGraphType<StringGraphType>>("username");
            Field<NonNullGraphType<StringGraphType>>("email");
            Field<NonNullGraphType<StringGraphType>>("password");
        }
    }
}
