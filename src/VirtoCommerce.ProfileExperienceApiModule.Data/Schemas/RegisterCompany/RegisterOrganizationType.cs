using System.Linq;
using GraphQL.Types;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.Xapi.Core.Extensions;
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
            ExtendableField<MemberAddressType>("address", resolve: context => (context.Source).Addresses?.FirstOrDefault());
            Field<StringGraphType>("phoneNumber").Resolve(context => (context.Source).Phones?.FirstOrDefault());
            Field<StringGraphType>("status");
            Field<StringGraphType>("createdBy");
            Field<StringGraphType>("ownerId");
            ExtendableFieldAsync<ListGraphType<DynamicPropertyValueType>>(
                "dynamicProperties",
                "Contact's dynamic property values",
                null,
                async context => await dynamicPropertyResolverService.LoadDynamicPropertyValues(context.Source, context.GetArgumentOrValue<string>("cultureName")));
        }
    }
}
