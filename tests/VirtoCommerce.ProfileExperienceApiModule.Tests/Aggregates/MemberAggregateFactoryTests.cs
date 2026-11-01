using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Organization;
using Xunit;

namespace VirtoCommerce.ProfileExperienceApiModule.Tests.Aggregates
{
    public class MemberAggregateFactoryTests
    {
        private readonly MemberAggregateFactory _factory = new();

        [Fact]
        public void Create_ContactAggregateForContact_WrapsMember()
        {
            var member = new Contact { Id = "member-1" };

            var result = _factory.Create<ContactAggregate>(member);

            Assert.NotNull(result);
            Assert.Same(member, result.Contact);
        }

        [Fact]
        public void Create_OrganizationAggregateForOrganization_WrapsMember()
        {
            var member = new Organization { Id = "member-1" };

            var result = _factory.Create<OrganizationAggregate>(member);

            Assert.NotNull(result);
            Assert.Same(member, result.Organization);
        }

        [Fact]
        public void Create_ContactAggregateForOrganization_ReturnsNull()
        {
            var result = _factory.Create<ContactAggregate>(new Organization { Id = "member-1" });

            Assert.Null(result);
        }

        [Fact]
        public void Create_OrganizationAggregateForContact_ReturnsNull()
        {
            var result = _factory.Create<OrganizationAggregate>(new Contact { Id = "member-1" });

            Assert.Null(result);
        }

        [Fact]
        public void Create_NullMember_ReturnsNull()
        {
            var result = _factory.Create<ContactAggregate>(null);

            Assert.Null(result);
        }
    }
}
