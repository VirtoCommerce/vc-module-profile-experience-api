using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.ExperienceApiModule.Core.Infrastructure;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models.RegisterCompany;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class RegisterCompanyCommand : ICommand<RegisterCompanyAggregate>
    {
        public string StoreId { get; set; }
        public Company Company { get; set; }
        public Owner Owner { get; set; }
        public ApplicationUser Account { get; set; }
    }
}
