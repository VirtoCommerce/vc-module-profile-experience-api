using GraphQL.Types;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class AddressTypeEnum : EnumerationGraphType
    {
        public AddressTypeEnum()
        {
            Name = "AddressTypeEnum";
            Add("Undefined", 0);
            Add("Billing", 1);
            Add("Shipping", 2);
            Add("Pickup", 4);
            Add("BillingAndShipping", 3);
        }
    }
}
