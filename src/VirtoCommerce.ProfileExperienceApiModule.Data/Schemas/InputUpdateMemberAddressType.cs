using GraphQL.Types;
using VirtoCommerce.CustomerModule.Core.Model;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputUpdateMemberAddressType : InputObjectGraphType
    {
        public InputUpdateMemberAddressType()
        {
            Field<NonNullGraphType<StringGraphType>>("memberId");
            Field<NonNullGraphType<ListGraphType<InputMemberAddressType>>>(nameof(Member.Addresses));
        }
    }
}
