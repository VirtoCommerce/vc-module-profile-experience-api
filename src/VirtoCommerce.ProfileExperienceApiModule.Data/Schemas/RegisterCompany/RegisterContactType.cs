using System.Linq;
using GraphQL.Types;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.ExperienceApiModule.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas.RegisterCompany
{
    public class RegisterContactType : ObjectGraphType
    {
        public RegisterContactType()
        {
            Field<NonNullGraphType<StringGraphType>>("Id");
            Field<NonNullGraphType<StringGraphType>>("firstName");
            Field<NonNullGraphType<StringGraphType>>("lastName");
            Field<StringGraphType>("middleName");
            Field<StringGraphType>("phoneNumber", resolve: context => (context.Source as Contact).Phones?.FirstOrDefault());
            Field<DateGraphType>("birthdate");
            Field<ListGraphType<DynamicPropertyValueType>>(nameof(Member.DynamicProperties));
            Field<StringGraphType>("status");
            Field<StringGraphType>("createdBy");
        }
    }
}
