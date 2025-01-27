using GraphQL.Types;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class AddressTypeEnum : EnumerationGraphType
    {
        public AddressTypeEnum()
        {
            Name = "AddressTypeEnum";
            Add(name: "Undefined", value: 0, description: "Undefined");
            Add(name: "Billing", value: 1, description: "Billing");
            Add(name: "Shipping", value: 2, description: "Shipping");
            Add(name: "Pickup", value: 4, description: "Pickup");
            Add(name: "BillingAndShipping", value: 3, description: "BillingAndShipping");
        }
    }
}
