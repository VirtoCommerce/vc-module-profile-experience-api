using GraphQL.Types;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.ExperienceApiModule.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas.RegisterCompany
{
    public class InputCompanyType : InputObjectGraphType
    {
        public InputCompanyType()
        {
            Field<NonNullGraphType<StringGraphType>>("name");
            Field<StringGraphType>("description");
            Field<ListGraphType<InputDynamicPropertyValueType>>(nameof(Member.DynamicProperties));
            Field<NonNullGraphType<InputMemberAddressType>>("address");
        }
    }
}
