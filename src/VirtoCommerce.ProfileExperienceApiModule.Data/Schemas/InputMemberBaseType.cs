using GraphQL.Types;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public abstract class InputMemberBaseType : InputObjectGraphType
    {
        protected InputMemberBaseType()
        {
            Field<StringGraphType>(nameof(Member.Id));
            Field<StringGraphType>(nameof(Member.Name));
            Field<StringGraphType>(nameof(Member.MemberType));
            Field<ListGraphType<InputMemberAddressType>>(nameof(Member.Addresses));
            Field<ListGraphType<StringGraphType>>(nameof(Member.Phones));
            Field<ListGraphType<StringGraphType>>(nameof(Member.Emails));
            Field<ListGraphType<StringGraphType>>(nameof(Member.Groups));
            Field<ListGraphType<InputDynamicPropertyValueType>>(nameof(Member.DynamicProperties));
        }
    }
}
