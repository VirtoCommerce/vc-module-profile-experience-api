namespace VirtoCommerce.ProfileExperienceApiModule.Data.Models.RegisterOrganization
{
    public class RegisterOrganizationResult
    {
        public virtual CustomerModule.Core.Model.Organization Organization { get; set; }
        public virtual CustomerModule.Core.Model.Contact Contact { get; set; }
        public virtual AccountCreationResult AccountCreationResult { get; set; }
    }
}
