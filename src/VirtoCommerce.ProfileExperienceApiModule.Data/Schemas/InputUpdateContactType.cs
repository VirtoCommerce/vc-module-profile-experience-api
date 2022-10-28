using System.Linq;
using GraphQL.Types;
using VirtoCommerce.CustomerModule.Core.Model;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class InputUpdateContactType : InputContactBaseType
    {
        public InputUpdateContactType()
        {
            Fields.First(x => x.Name == nameof(Member.Id)).Type = typeof(NonNullGraphType<StringGraphType>);
        }
    }
}
