using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.Xapi.Core.Schemas;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class NoteInputType : ExtendableInputObjectGraphType<Note>
    {
        public NoteInputType()
        {
            Field(x => x.Title);
            Field(x => x.Body);
            Field(x => x.OuterId, true);
        }
    }
}
