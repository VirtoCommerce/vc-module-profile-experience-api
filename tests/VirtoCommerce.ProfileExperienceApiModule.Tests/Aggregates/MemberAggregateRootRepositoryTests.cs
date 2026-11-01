using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;
using Xunit;

namespace VirtoCommerce.ProfileExperienceApiModule.Tests.Aggregates
{
    public class MemberAggregateRootRepositoryTests
    {
        private const string ContactId = "contact-1";

        [Fact]
        public async Task GetMemberAggregateRootByIdAsync_ChangedAggregate_LeavesTheServiceInstanceUntouched()
        {
            // The member service returns the instance it caches. updateContact writes the request into the aggregate
            // before validating it; when validation rejects the save, the cached member must still hold the stored
            // values, or every later read shows the rejected ones as if they were saved.
            var cachedContact = new Contact { Id = ContactId, FirstName = "Jane", LastName = "Doe" };
            var memberServiceMock = new Mock<IMemberService>();
            memberServiceMock
                .Setup(x => x.GetByIdAsync(ContactId, null, null))
                .ReturnsAsync(cachedContact);
            var repository = new MemberAggregateRootRepository(memberServiceMock.Object, new MemberAggregateFactory());

            var aggregate = await repository.GetMemberAggregateRootByIdAsync<ContactAggregate>(ContactId);
            aggregate.Contact.LastName = "Tester2";

            cachedContact.LastName.Should().Be("Doe");
            aggregate.Contact.Id.Should().Be(ContactId);
        }

        [Fact]
        public async Task GetMemberAggregateRootByIdAsync_UnknownMember_ReturnsNull()
        {
            // Unchanged: no member, no aggregate
            var repository = new MemberAggregateRootRepository(Mock.Of<IMemberService>(), new MemberAggregateFactory());

            var aggregate = await repository.GetMemberAggregateRootByIdAsync<ContactAggregate>(ContactId);

            aggregate.Should().BeNull();
        }
    }
}
