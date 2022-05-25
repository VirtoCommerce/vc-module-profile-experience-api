using GraphQL.Types;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.ExperienceApiModule.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas.RegisterCompany
{
    public class CompanyType : ObjectGraphType
    {
        public CompanyType()
        {
            Field<NonNullGraphType<StringGraphType>>("id");
            Field<NonNullGraphType<StringGraphType>>("name");
            Field<StringGraphType>("description");
            Field<ListGraphType<DynamicPropertyValueType>>(nameof(Member.DynamicProperties));
            Field<NonNullGraphType<MemberAddressType>>("address");
            Field<StringGraphType>("status");
        }
    }
}
