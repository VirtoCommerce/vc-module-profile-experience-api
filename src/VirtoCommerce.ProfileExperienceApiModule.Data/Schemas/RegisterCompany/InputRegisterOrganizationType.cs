using GraphQL.Types;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models.RegisterOrganization;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas.RegisterCompany
{
    public class InputRegisterOrganizationType : ExtendableInputObjectGraphType<RegisteredOrganization>
    {
        public InputRegisterOrganizationType()
        {
            Field<NonNullGraphType<StringGraphType>>("name");
            Field<StringGraphType>("description");
            Field<StringGraphType>("phoneNumber");
            Field<ListGraphType<InputDynamicPropertyValueType>>(nameof(Member.DynamicProperties));
            Field<InputMemberAddressType>("address").DeprecationReason("Use \"Addresses\" field. \"Address\" and \"Addresses\" fields will be automatically concatenated.");
            Field<ListGraphType<InputMemberAddressType>>("addresses");
        }
    }
}
