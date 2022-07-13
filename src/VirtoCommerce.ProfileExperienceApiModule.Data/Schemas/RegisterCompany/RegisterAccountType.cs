using GraphQL.Types;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas.RegisterCompany
{
    public class RegisterAccountType : ObjectGraphType
    {
        public RegisterAccountType()
        {
            Field<NonNullGraphType<StringGraphType>>("id");
            Field<NonNullGraphType<StringGraphType>>("username");
            Field<NonNullGraphType<StringGraphType>>("email");
            Field<StringGraphType>("status");
            Field<StringGraphType>("createdBy");
        }
    }
}
