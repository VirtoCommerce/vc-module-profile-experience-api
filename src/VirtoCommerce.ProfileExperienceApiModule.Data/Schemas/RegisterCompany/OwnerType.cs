using GraphQL.Types;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.ExperienceApiModule.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas.RegisterCompany
{
    public class OwnerType : ObjectGraphType
    {
        public OwnerType()
        {
            Field<NonNullGraphType<StringGraphType>>("Id");
            Field<NonNullGraphType<StringGraphType>>("firstName");
            Field<NonNullGraphType<StringGraphType>>("lastName");
            Field<StringGraphType>("middleName");
            Field<NonNullGraphType<StringGraphType>>("phoneNumber");
            Field<DateGraphType>("birthdate");
            Field<ListGraphType<DynamicPropertyValueType>>(nameof(Member.DynamicProperties));
            Field<StringGraphType>("status");
            Field<StringGraphType>("createdBy");
        }
    }
}
