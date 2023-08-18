using System.Collections.Generic;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Models.RegisterOrganization
{
    public class AccountCreationResult
    {
        public string AccountId { get; set; }
        public string AccountName { get; set; }
        public string Email { get; set; }
        public bool RequireEmailVerification { get; set; }
        public List<RegistrationError> Errors { get; set; }
        public bool Succeeded { get; set; }
    }
}
