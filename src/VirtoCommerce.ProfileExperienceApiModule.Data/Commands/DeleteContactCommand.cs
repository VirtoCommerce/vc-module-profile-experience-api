using VirtoCommerce.ExperienceApiModule.Core.Infrastructure;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class DeleteContactCommand : ICommand<bool>
    {
        public DeleteContactCommand(string contactId)
        {
            ContactId = contactId;
        }

        public string ContactId { get; set; }
    }
}
