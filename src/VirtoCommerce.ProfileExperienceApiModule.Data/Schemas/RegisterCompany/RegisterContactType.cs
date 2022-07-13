using System.Linq;
using GraphQL.Types;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.ExperienceApiModule.Core.Extensions;
using VirtoCommerce.ExperienceApiModule.Core.Helpers;
using VirtoCommerce.ExperienceApiModule.Core.Schemas;
using VirtoCommerce.ExperienceApiModule.Core.Services;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas.RegisterCompany
{
    public class RegisterContactType : ExtendableGraphType<Contact>
    {
        public RegisterContactType(IDynamicPropertyResolverService dynamicPropertyResolverService)
        {
            Field<NonNullGraphType<StringGraphType>>("Id");
            Field<NonNullGraphType<StringGraphType>>("firstName");
            Field<NonNullGraphType<StringGraphType>>("lastName");
            Field<StringGraphType>("middleName");
            Field<StringGraphType>("phoneNumber", resolve: context => (context.Source).Phones?.FirstOrDefault());
            Field<DateGraphType>("birthdate");
            Field<StringGraphType>("status");
            Field<StringGraphType>("createdBy");
            ExtendableField<NonNullGraphType<ListGraphType<DynamicPropertyValueType>>>(
                "dynamicProperties",
                "Contact's dynamic property values",
                QueryArgumentPresets.GetArgumentForDynamicProperties(),
                context => dynamicPropertyResolverService.LoadDynamicPropertyValues(context.Source, context.GetArgumentOrValue<string>("cultureName")));
        }
    }
}
