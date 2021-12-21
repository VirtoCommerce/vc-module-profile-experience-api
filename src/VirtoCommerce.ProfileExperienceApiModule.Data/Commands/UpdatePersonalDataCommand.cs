using Microsoft.AspNetCore.Identity;
using VirtoCommerce.ExperienceApiModule.Core.Infrastructure;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class UpdatePersonalDataCommand : ICommand<IdentityResult>
    {
        public string UserId { get; set; }
        public PersonalData PersonalData { get; set; }
    }
}
