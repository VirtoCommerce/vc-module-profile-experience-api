using GraphQL.Types;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models.RegisterOrganization;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas.RegisterCompany
{
    public class InputRegisterContactType : ExtendableInputObjectGraphType<RegisteredContact>
    {
        public InputRegisterContactType()
        {
            Field<NonNullGraphType<StringGraphType>>("firstName");
            Field<NonNullGraphType<StringGraphType>>("lastName");
            Field<StringGraphType>("middleName");
            Field<StringGraphType>("phoneNumber");
            Field<DateGraphType>("birthdate");
            Field<InputMemberAddressType>("address");
            Field<StringGraphType>("about");
            Field<ListGraphType<InputDynamicPropertyValueType>>(nameof(Member.DynamicProperties));
        }
    }
}
