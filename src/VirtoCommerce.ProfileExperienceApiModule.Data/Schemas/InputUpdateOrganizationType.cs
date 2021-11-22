using System.Linq;
using GraphQL.Types;
using VirtoCommerce.CustomerModule.Core.Model;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputUpdateOrganizationType : InputMemberBaseType
    {
        public InputUpdateOrganizationType()
        {
            Fields.FirstOrDefault(x => x.Name == nameof(Member.Id)).Type = typeof(NonNullGraphType<StringGraphType>);
        }
    }
}
