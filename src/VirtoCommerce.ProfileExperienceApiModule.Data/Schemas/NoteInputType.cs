using GraphQL.Types;
using VirtoCommerce.CustomerModule.Core.Model;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class NoteInputType : InputObjectGraphType<Note>
    {
        public NoteInputType()
        {
            Field(x => x.Title);
            Field(x => x.Body);
            Field(x => x.OuterId, true);
        }
    }
}
