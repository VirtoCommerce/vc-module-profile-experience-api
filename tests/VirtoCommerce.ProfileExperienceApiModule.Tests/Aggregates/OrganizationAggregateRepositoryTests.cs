using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Organization;
using Xunit;

namespace VirtoCommerce.ProfileExperienceApiModule.Tests.Aggregates
{
    public class OrganizationAggregateRepositoryTests
    {
        private const string OrganizationId = "organization-1";

        [Fact]
        public async Task GetOrganizationsByIdsAsync_ChangedAggregate_LeavesTheServiceInstanceUntouched()
        {
            // The same rule as the by-id read: the member service returns the instances it caches, so the aggregates
            // must work on copies, or a change that is never saved shows up in every later read
            var cachedOrganization = new Organization { Id = OrganizationId, Name = "Acme" };
            var memberServiceMock = new Mock<IMemberService>();
            memberServiceMock
                .Setup(x => x.GetByIdsAsync(It.IsAny<string[]>(), null, It.IsAny<string[]>()))
                .ReturnsAsync(new Member[] { cachedOrganization });
            var repository = new OrganizationAggregateRepository(memberServiceMock.Object, new MemberAggregateFactory());

            var aggregate = (await repository.GetOrganizationsByIdsAsync([OrganizationId])).Single();
            aggregate.Organization.Name = "Acme2";

            cachedOrganization.Name.Should().Be("Acme");
            aggregate.Organization.Id.Should().Be(OrganizationId);
        }
    }
}
