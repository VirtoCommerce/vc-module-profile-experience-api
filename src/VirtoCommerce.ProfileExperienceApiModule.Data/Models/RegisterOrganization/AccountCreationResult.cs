using System.Collections.Generic;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Models.RegisterOrganization
{
    public class AccountCreationResult
    {
        public string AccountName { get; set; }
        public bool RequireEmailVerification { get; set; }
        public List<RegistrationError> Errors { get; set; }
        public bool Succeeded { get; set; }
    }
}
