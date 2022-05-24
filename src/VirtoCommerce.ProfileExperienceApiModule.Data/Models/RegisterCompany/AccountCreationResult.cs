using System.Collections.Generic;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Models.RegisterCompany
{
    public class AccountCreationResult
    {
        public string AccountName { get; set; }
        public List<string> Errors  { get; set; }
        public bool Succeeded { get; set; }
    }
}
