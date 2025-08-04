using GraphQL.Types;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputCreateOrganizationType : ExtendableInputObjectGraphType
    {
        public InputCreateOrganizationType()
        {
            Field<StringGraphType>(nameof(Organization.Name));
            Field<ListGraphType<InputMemberAddressType>>(nameof(Organization.Addresses));
            Field<ListGraphType<InputDynamicPropertyValueType>>(nameof(Organization.DynamicProperties));
        }
    }
}
