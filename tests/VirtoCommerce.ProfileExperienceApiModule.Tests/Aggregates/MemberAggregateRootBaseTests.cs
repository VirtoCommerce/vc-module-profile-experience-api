using System.Collections.Generic;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Organization;
using Xunit;

namespace VirtoCommerce.ProfileExperienceApiModule.Tests.Aggregates
{
    public class MemberAggregateRootBaseTests
    {
        private static Address CreateAddress(
            string firstName = "John",
            string lastName = "Doe",
            string city = "New York",
            string line1 = "123 Main St",
            string line2 = "",
            string countryCode = "US",
            string regionId = "NY",
            string postalCode = "10001",
            string phone = "+1234567890",
            string email = "john@example.com",
            string key = null)
        {
            return new Address
            {
                Key = key,
                FirstName = firstName,
                LastName = lastName,
                City = city,
                Line1 = line1,
                Line2 = line2,
                CountryCode = countryCode,
                RegionId = regionId,
                PostalCode = postalCode,
                Phone = phone,
                Email = email,
            };
        }

        private static OrganizationAggregate CreateAggregate(params Address[] existingAddresses)
        {
            var organization = new Organization
            {
                Addresses = new List<Address>(existingAddresses),
            };

            return new OrganizationAggregate { Member = organization };
        }

        [Fact]
        public void UpdateAddresses_NewAddress_ShouldAdd()
        {
            // Arrange
            var aggregate = CreateAggregate();
            var newAddress = CreateAddress();

            // Act
            aggregate.UpdateAddresses([newAddress]);

            // Assert
            Assert.Single(aggregate.Member.Addresses);
        }

        [Fact]
        public void UpdateAddresses_DuplicateAddress_ShouldSkip()
        {
            // Arrange
            var existing = CreateAddress(key: "addr-1");
            var aggregate = CreateAggregate(existing);

            var duplicate = CreateAddress(); // same field values, no key

            // Act
            aggregate.UpdateAddresses(new List<Address> { duplicate });

            // Assert
            Assert.Single(aggregate.Member.Addresses);
            Assert.Equal("addr-1", aggregate.Member.Addresses[0].Key);
        }
    }
}
