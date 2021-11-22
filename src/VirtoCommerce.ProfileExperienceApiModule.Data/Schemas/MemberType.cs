using GraphQL.Types;
using VirtoCommerce.ExperienceApiModule.Core.Extensions;
using VirtoCommerce.ExperienceApiModule.Core.Helpers;
using VirtoCommerce.ExperienceApiModule.Core.Schemas;
using VirtoCommerce.ExperienceApiModule.Core.Services;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class MemberType : ExtendableGraphType<MemberAggregateRootBase>
    {
        public MemberType(IDynamicPropertyResolverService dynamicPropertyResolverService)
        {
            Field(x => x.Member.Id);
            Field(x => x.Member.MemberType);
            Field(x => x.Member.Name, true);
            Field(x => x.Member.OuterId, true);
            ExtendableField<ListGraphType<MemberAddressType>>("addresses", resolve: context => context.Source.Member.Addresses);
            ExtendableField<NonNullGraphType<ListGraphType<DynamicPropertyValueType>>>(
                "dynamicProperties",
                "Contact's dynamic property values",
                QueryArgumentPresets.GetArgumentForDynamicProperties(),
                context => dynamicPropertyResolverService.LoadDynamicPropertyValues(context.Source.Member, context.GetArgumentOrValue<string>("cultureName")));
            Field("phones", x => x.Member.Phones);
        }
    }
}
