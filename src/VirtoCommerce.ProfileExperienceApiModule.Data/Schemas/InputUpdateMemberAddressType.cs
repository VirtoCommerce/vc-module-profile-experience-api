using GraphQL.Types;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputUpdateMemberAddressType : ExtendableInputObjectGraphType
    {
        public InputUpdateMemberAddressType()
        {
            Field<NonNullGraphType<StringGraphType>>("memberId");
            Field<NonNullGraphType<ListGraphType<InputMemberAddressType>>>(nameof(Member.Addresses));
        }
    }
}
