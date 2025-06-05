namespace VirtoCommerce.ProfileExperienceApiModule.Data.Commands
{
    public class UpdateContactCommand : ContactCommand
    {
        public string UserId { get; set; }
        public string OrganizationId { get; set; }
    }
}
