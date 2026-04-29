using VirtoCommerce.ProfileExperienceApiModule.Data.Models;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class AddressDuplicatedResultType : ExtendableGraphType<CheckDuplicateAddressResult>
    {
        public AddressDuplicatedResultType()
        {
            Field(x => x.IsDuplicated).Description("Indicates whether the address is a duplicate of an existing address.");
        }
    }
}
