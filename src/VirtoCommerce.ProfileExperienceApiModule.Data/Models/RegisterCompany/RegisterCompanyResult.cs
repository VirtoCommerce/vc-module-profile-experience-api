namespace VirtoCommerce.ProfileExperienceApiModule.Data.Models.RegisterCompany
{
    public class RegisterCompanyResult
    {
        public virtual CustomerModule.Core.Model.Organization Company { get; set; }
        public virtual CustomerModule.Core.Model.Contact Contact { get; set; }
        public virtual AccountCreationResult AccountCreationResult { get; set; }
    }
}
