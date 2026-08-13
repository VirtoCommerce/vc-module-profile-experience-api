using System;
using System.Linq;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models.RegisterOrganization;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;
using VirtoCommerce.ProfileExperienceApiModule.Web;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.Xapi.Core.Models;
using Xunit;

namespace VirtoCommerce.ProfileExperienceApiModule.Tests;

public class ProfileExperienceApiModuleMapperTests
{
    private static readonly IMapper _legacyMapper = new MapperConfiguration(cfg =>
        cfg.AddProfile<LegacyProfileMappingProfile>()).CreateMapper();

    private readonly ProfileExperienceApiModuleMapper _mapper = new();

    [Theory]
    [InlineData(nameof(IProfileExperienceApiModuleMapper.ToCustomer))]
    [InlineData(nameof(IProfileExperienceApiModuleMapper.ToTaxAddress))]
    [InlineData("ToOrganization_CreateOrganizationCommand")]
    [InlineData("ToOrganization_RegisteredOrganization")]
    [InlineData("ToContact_CreateContactCommand")]
    [InlineData("ToContact_RegisteredContact")]
    public void ToXxx_NullSource_ReturnsNull(string methodKey)
    {
        object result = methodKey switch
        {
            nameof(IProfileExperienceApiModuleMapper.ToCustomer) => _mapper.ToCustomer(null),
            nameof(IProfileExperienceApiModuleMapper.ToTaxAddress) => _mapper.ToTaxAddress(null),
            "ToOrganization_CreateOrganizationCommand" => _mapper.ToOrganization((CreateOrganizationCommand)null),
            "ToOrganization_RegisteredOrganization" => _mapper.ToOrganization((RegisteredOrganization)null),
            "ToContact_CreateContactCommand" => _mapper.ToContact((CreateContactCommand)null),
            "ToContact_RegisteredContact" => _mapper.ToContact((RegisteredContact)null),
            _ => throw new ArgumentOutOfRangeException(nameof(methodKey)),
        };

        result.Should().BeNull();
    }

    [Fact]
    public void MapTo_UpdateContactCommand_NullSource_DoesNotThrow()
    {
        var target = new Contact { Name = "unchanged" };

        _mapper.MapTo(null, target);

        target.Name.Should().Be("unchanged");
    }

    [Fact]
    public void MapTo_UpdateOrganizationCommand_NullSource_DoesNotThrow()
    {
        var target = new Organization { Name = "unchanged" };

        _mapper.MapTo(null, target);

        target.Name.Should().Be("unchanged");
    }

    [Fact]
    public void ToOrganization_CreateOrganizationCommand_CopiesNameAndAddresses_IgnoresDynamicProperties()
    {
        var source = new CreateOrganizationCommand
        {
            Name = "Acme",
            Addresses = [new Address { City = "Seattle" }],
            DynamicProperties = [new DynamicPropertyValue { PropertyName = "prop" }],
        };

        var result = _mapper.ToOrganization(source);

        result.Name.Should().Be("Acme");
        result.Addresses.Should().BeSameAs(source.Addresses);
        result.DynamicProperties.Should().BeNull();
    }

    [Fact]
    public void ToContact_CreateContactCommand_CopiesFields_IgnoresIdAndDynamicProperties()
    {
        var source = new CreateContactCommand
        {
            Id = "should-be-ignored",
            Name = "Contact 1",
            FirstName = "John",
            LastName = "Doe",
            Addresses = [new Address { City = "Seattle" }],
            Phones = ["123"],
            Emails = ["john@example.com"],
            Groups = ["vip"],
            Organizations = ["org-1"],
            DynamicProperties = [new DynamicPropertyValue { PropertyName = "prop" }],
        };

        var result = _mapper.ToContact(source);

        result.Name.Should().Be("Contact 1");
        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
        result.Addresses.Should().BeSameAs(source.Addresses);
        result.Phones.Should().BeSameAs(source.Phones);
        result.Emails.Should().BeSameAs(source.Emails);
        result.Groups.Should().BeSameAs(source.Groups);
        result.Organizations.Should().BeSameAs(source.Organizations);

        result.Id.Should().BeNull();
        result.DynamicProperties.Should().BeNull();
    }

    [Fact]
    public void ToContact_RegisteredContact_BuildsPhonesAndAddressFromSingleValues()
    {
        var source = new RegisteredContact
        {
            FirstName = "Jane",
            LastName = "Roe",
            PhoneNumber = "555-0100",
            Address = new Address { City = "Portland" },
        };

        var result = _mapper.ToContact(source);

        result.FirstName.Should().Be("Jane");
        result.LastName.Should().Be("Roe");
        result.Phones.Should().ContainSingle().Which.Should().Be("555-0100");
        result.Addresses.Should().ContainSingle().Which.Should().BeSameAs(source.Address);
    }

    [Fact]
    public void ToOrganization_RegisteredOrganization_AppendsSingleAddressToAddressList()
    {
        var listedAddress = new Address { City = "Boston" };
        var singleAddress = new Address { City = "Chicago" };
        var source = new RegisteredOrganization
        {
            Name = "Acme",
            PhoneNumber = "555-0200",
            Addresses = [listedAddress],
            Address = singleAddress,
        };

        var result = _mapper.ToOrganization(source);

        result.Name.Should().Be("Acme");
        result.Phones.Should().ContainSingle().Which.Should().Be("555-0200");
        result.Addresses.Should().Equal(listedAddress, singleAddress);
    }

    [Fact]
    public void MapTo_UpdateContactCommand_ConditionalFieldsNull_LeaveTargetUnchanged()
    {
        var target = new Contact
        {
            Emails = ["existing@example.com"],
            Phones = ["existing-phone"],
            Groups = ["existing-group"],
            Organizations = ["existing-org"],
            Addresses = [new Address { City = "Existing" }],
        };
        var source = new UpdateContactCommand
        {
            Id = "contact-1",
            Name = "New Name",
            Emails = null,
            Phones = null,
            Groups = null,
            Organizations = null,
            Addresses = null,
        };

        _mapper.MapTo(source, target);

        target.Id.Should().Be("contact-1");
        target.Name.Should().Be("New Name");
        target.Emails.Should().Equal("existing@example.com");
        target.Phones.Should().Equal("existing-phone");
        target.Groups.Should().Equal("existing-group");
        target.Organizations.Should().Equal("existing-org");
        target.Addresses.Should().ContainSingle().Which.City.Should().Be("Existing");
    }

    [Fact]
    public void MapTo_UpdateContactCommand_ConditionalFieldsSet_OverwriteTarget()
    {
        var target = new Contact { Emails = ["old@example.com"] };
        var source = new UpdateContactCommand
        {
            Id = "contact-1",
            Emails = ["new@example.com"],
        };

        _mapper.MapTo(source, target);

        target.Emails.Should().Equal("new@example.com");
    }

    [Fact]
    public void MapTo_UpdateOrganizationCommand_OnlySpecifiedOptionalFieldsAreApplied()
    {
        var target = new Organization { Name = "Old Name", MemberType = "Organization" };
        var source = new UpdateOrganizationCommand
        {
            Id = "org-1",
            Name = new Optional<string>("New Name", isSpecified: true),
            // MemberType left unspecified on purpose.
        };

        _mapper.MapTo(source, target);

        target.Id.Should().Be("org-1");
        target.Name.Should().Be("New Name");
        target.MemberType.Should().Be("Organization");
    }

    [Fact]
    public void ToCustomer_MapsContactToTaxCustomer_IncludingNestedAddresses()
    {
        var source = new Contact
        {
            Id = "contact-1",
            Name = "Contact 1",
            FirstName = "John",
            LastName = "Doe",
            Phones = ["123"],
            Emails = ["john@example.com"],
            Addresses = [new Address { City = "Seattle", CountryCode = "US" }],
        };

        var result = _mapper.ToCustomer(source);

        result.Id.Should().Be("contact-1");
        result.Name.Should().Be("Contact 1");
        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
        result.Phones.Should().BeSameAs(source.Phones);
        result.Emails.Should().BeSameAs(source.Emails);
        result.Addresses.Should().ContainSingle();
        result.Addresses[0].City.Should().Be("Seattle");
        result.Addresses[0].CountryCode.Should().Be("US");
    }

    [Fact]
    public void ToTaxAddress_CopiesAllMatchingFields()
    {
        var source = new Address
        {
            Key = "addr-1",
            Name = "Home",
            CountryCode = "US",
            CountryName = "United States",
            City = "Seattle",
            PostalCode = "98101",
            Line1 = "1 Main St",
            RegionName = "WA",
            FirstName = "John",
            LastName = "Doe",
            Phone = "123",
            Email = "john@example.com",
            IsDefault = true,
        };

        var result = _mapper.ToTaxAddress(source);

        result.Key.Should().Be("addr-1");
        result.Name.Should().Be("Home");
        result.CountryCode.Should().Be("US");
        result.CountryName.Should().Be("United States");
        result.City.Should().Be("Seattle");
        result.PostalCode.Should().Be("98101");
        result.Line1.Should().Be("1 Main St");
        result.RegionName.Should().Be("WA");
        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
        result.Phone.Should().Be("123");
        result.Email.Should().Be("john@example.com");
        result.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void ToOrganization_CreateOrganizationCommand_ProducesSameResultAsLegacyAutoMapperConventionMap()
    {
        var source = new CreateOrganizationCommand
        {
            Name = "Acme",
            Addresses = [new Address { City = "Seattle" }],
        };

        var expected = _legacyMapper.Map<Organization>(source);
        var actual = _mapper.ToOrganization(source);

        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ToOrganization_RegisteredOrganization_ProducesSameResultAsLegacyAutoMapperConventionMap()
    {
        var source = new RegisteredOrganization
        {
            Name = "Acme",
            Description = "desc",
            PhoneNumber = "555-0200",
            Addresses = [new Address { City = "Boston" }],
            Address = new Address { City = "Chicago" },
        };

        var expected = _legacyMapper.Map<Organization>(source);
        var actual = _mapper.ToOrganization(source);

        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ToContact_CreateContactCommand_ProducesSameResultAsLegacyAutoMapperConventionMap()
    {
        var source = new CreateContactCommand
        {
            Id = "should-be-ignored",
            Name = "Contact 1",
            FirstName = "John",
            LastName = "Doe",
            MiddleName = "M",
            FullName = "John M Doe",
            Salutation = "Mr",
            About = "bio",
            PhotoUrl = "http://example.com/photo.png",
            TimeZone = "UTC",
            DefaultLanguage = "en-US",
            CurrencyCode = "USD",
            Addresses = [new Address { City = "Seattle" }],
            Phones = ["123"],
            Emails = ["john@example.com"],
            Groups = ["vip"],
            Organizations = ["org-1"],
        };

        var expected = _legacyMapper.Map<Contact>(source);
        var actual = _mapper.ToContact(source);

        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ToContact_RegisteredContact_ProducesSameResultAsLegacyAutoMapperConventionMap()
    {
        var source = new RegisteredContact
        {
            FirstName = "Jane",
            LastName = "Roe",
            MiddleName = "M",
            Birthdate = new DateTime(1990, 1, 1),
            About = "bio",
            PhoneNumber = "555-0100",
            Address = new Address { City = "Portland" },
        };

        var expected = _legacyMapper.Map<Contact>(source);
        var actual = _mapper.ToContact(source);

        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ToCustomer_ProducesSameResultAsLegacyAutoMapperConventionMap()
    {
        var source = new Contact
        {
            Id = "contact-1",
            Name = "Contact 1",
            FirstName = "John",
            MiddleName = "M",
            LastName = "Doe",
            OuterId = "outer-1",
            Phones = ["123"],
            Emails = ["john@example.com"],
            Groups = ["vip"],
            BirthDate = new DateTime(1990, 1, 1),
            DefaultLanguage = "en-US",
            TimeZone = "UTC",
            Organizations = ["org-1"],
            TaxPayerId = "tax-1",
            Addresses = [new Address { City = "Seattle", CountryCode = "US", Key = "addr-1" }],
        };

        var expected = _legacyMapper.Map<TaxModule.Core.Model.Customer>(source);
        var actual = _mapper.ToCustomer(source);

        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void MapTo_UpdateContactCommand_ProducesSameResultAsLegacyAutoMapperConventionMap()
    {
        var source = new UpdateContactCommand
        {
            Id = "contact-1",
            Name = "Contact 1",
            FirstName = "John",
            LastName = "Doe",
            About = "bio",
            Addresses = [new Address { City = "Seattle" }],
            Phones = ["123"],
            Emails = ["john@example.com"],
            Groups = ["vip"],
            Organizations = ["org-1"],
        };

        var expected = new Contact { Emails = ["old@example.com"] };
        _legacyMapper.Map(source, expected);

        var actual = new Contact { Emails = ["old@example.com"] };
        _mapper.MapTo(source, actual);

        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void MapTo_UpdateContactCommand_WithNullConditionalFields_ProducesSameResultAsLegacyAutoMapperConventionMap()
    {
        var source = new UpdateContactCommand
        {
            Id = "contact-1",
            Name = "Contact 1",
            Addresses = null,
            Emails = null,
            Groups = null,
            Phones = null,
            Organizations = null,
        };

        var expected = new Contact { Emails = ["old@example.com"], Phones = ["old-phone"] };
        _legacyMapper.Map(source, expected);

        var actual = new Contact { Emails = ["old@example.com"], Phones = ["old-phone"] };
        _mapper.MapTo(source, actual);

        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ToTaxAddress_ProducesSameResultAsLegacyAutoMapperConventionMap()
    {
        var source = new Address
        {
            Key = "addr-1",
            Name = "Home",
            CountryCode = "US",
            CountryName = "United States",
            City = "Seattle",
            PostalCode = "98101",
            Line1 = "1 Main St",
            RegionName = "WA",
            FirstName = "John",
            LastName = "Doe",
            Phone = "123",
            Email = "john@example.com",
            IsDefault = true,
        };

        var expected = _legacyMapper.Map<TaxModule.Core.Model.Address>(source);
        var actual = _mapper.ToTaxAddress(source);

        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void Module_Initialize_Registers_ProfileExperienceApiModuleMapper_AsSingleton()
    {
        var services = new ServiceCollection();
        var module = new Module { Configuration = new ConfigurationBuilder().Build() };

        module.Initialize(services);

        var descriptor = services.SingleOrDefault(x => x.ServiceType == typeof(IProfileExperienceApiModuleMapper));

        descriptor.Should().NotBeNull();
        descriptor.ImplementationType.Should().Be<ProfileExperienceApiModuleMapper>();
        descriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }
}
