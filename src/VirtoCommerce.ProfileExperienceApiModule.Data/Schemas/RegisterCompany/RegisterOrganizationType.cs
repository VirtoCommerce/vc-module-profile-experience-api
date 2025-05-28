using System.Linq;
using GraphQL.Types;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Helpers;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.Xapi.Core.Services;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas.RegisterCompany
{
    public class RegisterOrganizationType : ExtendableGraphType<Organization>
    {
        public RegisterOrganizationType(IDynamicPropertyResolverService dynamicPropertyResolverService)
        {
            Field<NonNullGraphType<StringGraphType>>("id");
            Field<NonNullGraphType<StringGraphType>>("name");
            Field<StringGraphType>("description");
            ExtendableField<MemberAddressType>("address",
                resolve: context => (context.Source).Addresses?.FirstOrDefault(),
                description: "Returns first organization address.",
                deprecationReason: "Use addresses field instead.");
            ExtendableField<ListGraphType<MemberAddressType>>("addresses",
                resolve: context => (context.Source).Addresses,
                description: "Organization's addresses");
            Field<StringGraphType>("phoneNumber").Resolve(context => (context.Source).Phones?.FirstOrDefault());
            Field<StringGraphType>("status");
            Field<StringGraphType>("createdBy");
            Field<StringGraphType>("ownerId");
            ExtendableFieldAsync<ListGraphType<DynamicPropertyValueType>>(
                "dynamicProperties",
                "Organization's dynamic property values",
                QueryArgumentPresets.GetArgumentForDynamicProperties(),
                async context => await dynamicPropertyResolverService.LoadDynamicPropertyValues(context.Source, context.GetArgumentOrValue<string>("cultureName")));
        }
    }
}
