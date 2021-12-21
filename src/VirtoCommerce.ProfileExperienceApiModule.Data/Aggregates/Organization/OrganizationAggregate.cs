namespace VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Organization
{
    public class OrganizationAggregate : MemberAggregateRootBase
    {
        public CustomerModule.Core.Model.Organization Organization => Member as CustomerModule.Core.Model.Organization;
    }
}
