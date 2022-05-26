using GraphQL.Types;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas.RegisterCompany
{
    public class AccountType : ObjectGraphType
    {
        public AccountType()
        {
            Field<NonNullGraphType<StringGraphType>>("id");
            Field<NonNullGraphType<StringGraphType>>("username");
            Field<NonNullGraphType<StringGraphType>>("email");
            Field<StringGraphType>("status");
            Field<StringGraphType>("createdBy");
        }
    }
}
