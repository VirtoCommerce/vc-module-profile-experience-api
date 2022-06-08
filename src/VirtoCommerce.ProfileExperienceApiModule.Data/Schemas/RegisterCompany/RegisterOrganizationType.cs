using System.Linq;
using GraphQL.Types;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.ExperienceApiModule.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas.RegisterCompany
{
    public class RegisterOrganizationType : ObjectGraphType
    {
        public RegisterOrganizationType()
        {
            Field<NonNullGraphType<StringGraphType>>("id");
            Field<NonNullGraphType<StringGraphType>>("name");
            Field<StringGraphType>("description");
            Field<ListGraphType<DynamicPropertyValueType>>(nameof(Member.DynamicProperties));
            Field<MemberAddressType>("address", resolve: context => (context.Source as Organization).Addresses?.FirstOrDefault());
            Field<StringGraphType>("status");
            Field<StringGraphType>("createdBy");
            Field<StringGraphType>("ownerId");
        }
    }
}
