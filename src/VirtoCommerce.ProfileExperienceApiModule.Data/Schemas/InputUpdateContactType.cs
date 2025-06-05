using System.Linq;
using GraphQL.Types;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputUpdateContactType : InputContactBaseType
    {
        public InputUpdateContactType()
        {
            Fields.First(x => x.Name == nameof(UpdateContactCommand.Id)).Type = typeof(NonNullGraphType<StringGraphType>);
        }
    }
}
