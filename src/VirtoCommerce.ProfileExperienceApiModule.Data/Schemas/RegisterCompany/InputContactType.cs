using GraphQL.Types;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.ExperienceApiModule.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas.RegisterCompany
{
    public class InputContactType : InputObjectGraphType
    {
        public InputContactType()
        {
            Field<NonNullGraphType<StringGraphType>>("firstName");
            Field<NonNullGraphType<StringGraphType>>("lastName");
            Field<StringGraphType>("middleName");
            Field<NonNullGraphType<StringGraphType>>("phoneNumber");
            Field<DateGraphType>("birthdate");
            Field<StringGraphType>("about");
            Field<ListGraphType<InputDynamicPropertyValueType>>(nameof(Member.DynamicProperties));
        }
    }
}
