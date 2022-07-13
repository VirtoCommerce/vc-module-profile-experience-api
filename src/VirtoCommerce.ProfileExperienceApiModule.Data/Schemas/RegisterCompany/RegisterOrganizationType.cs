using System.Linq;
using GraphQL.Types;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.ExperienceApiModule.Core.Extensions;
using VirtoCommerce.ExperienceApiModule.Core.Helpers;
using VirtoCommerce.ExperienceApiModule.Core.Schemas;
using VirtoCommerce.ExperienceApiModule.Core.Services;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas.RegisterCompany
{
    public class RegisterOrganizationType : ExtendableGraphType<Organization>
    {
        public RegisterOrganizationType(IDynamicPropertyResolverService dynamicPropertyResolverService)
        {
            Field<NonNullGraphType<StringGraphType>>("id");
            Field<NonNullGraphType<StringGraphType>>("name");
            Field<StringGraphType>("description");
            Field<MemberAddressType>("address", resolve: context => (context.Source).Addresses?.FirstOrDefault());
            Field<StringGraphType>("status");
            Field<StringGraphType>("createdBy");
            Field<StringGraphType>("ownerId");
            ExtendableField<NonNullGraphType<ListGraphType<DynamicPropertyValueType>>>(
                "dynamicProperties",
                "Contact's dynamic property values",
                QueryArgumentPresets.GetArgumentForDynamicProperties(),
                context => dynamicPropertyResolverService.LoadDynamicPropertyValues(context.Source, context.GetArgumentOrValue<string>("cultureName")));
        }
    }
}
