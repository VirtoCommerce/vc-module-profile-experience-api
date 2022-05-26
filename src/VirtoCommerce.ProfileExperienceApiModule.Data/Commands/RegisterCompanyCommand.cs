using VirtoCommerce.ExperienceApiModule.Core.Infrastructure;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models.RegisterCompany;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class RegisterCompanyCommand : ICommand<RegisterCompanyResult>
    {
        public string StoreId { get; set; }
        public Company Company { get; set; }
        public Owner Contact { get; set; }
        public Account Account { get; set; }
    }
}
