using GraphQL.Types;
using VirtoCommerce.CustomerModule.Core.Model;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputDeleteMemberAddressType : InputObjectGraphType
    {
        public InputDeleteMemberAddressType()
        {
            Field<NonNullGraphType<StringGraphType>>("memberId");
            Field<NonNullGraphType<ListGraphType<InputMemberAddressType>>>(nameof(Member.Addresses));
        }
    }
}
