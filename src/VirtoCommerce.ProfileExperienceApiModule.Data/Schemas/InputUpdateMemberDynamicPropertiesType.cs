using GraphQL.Types;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputUpdateMemberDynamicPropertiesType : ExtendableInputObjectGraphType
    {
        public InputUpdateMemberDynamicPropertiesType()
        {
            Field<NonNullGraphType<StringGraphType>>("memberId");
            Field<NonNullGraphType<ListGraphType<InputDynamicPropertyValueType>>>(nameof(Member.DynamicProperties));
        }
    }
}
