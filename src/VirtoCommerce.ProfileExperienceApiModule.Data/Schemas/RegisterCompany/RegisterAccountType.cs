using GraphQL.Types;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas.RegisterCompany
{
    public class RegisterAccountType : ExtendableGraphType<ApplicationUser>
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
