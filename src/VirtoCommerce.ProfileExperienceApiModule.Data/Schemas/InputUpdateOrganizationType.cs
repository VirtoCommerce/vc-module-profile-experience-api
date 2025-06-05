using System.Linq;
using GraphQL.Types;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputUpdateOrganizationType : InputMemberBaseType
    {
        public InputUpdateOrganizationType()
        {
            Fields.First(x => x.Name == nameof(UpdateOrganizationCommand.Id)).Type = typeof(NonNullGraphType<StringGraphType>);
        }
    }
}
