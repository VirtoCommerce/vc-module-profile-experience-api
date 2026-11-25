using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Configuration;
using VirtoCommerce.ProfileExperienceApiModule.Data.Extensions;
using Xunit;

namespace VirtoCommerce.ProfileExperienceApiModule.Tests.Extensions;

public class FrontendSecurityOptionsExtensionsTests
{
    [Theory]
    [InlineData("Organization employee")]
    [InlineData("Purchasing agent")]
    [InlineData("Organization maintainer")]
    public void IsRoleAllowed_WhitelistNotConfigured_AllowsDefaultRole(string roleName)
    {
        // Arrange
        var role = new Role { Id = "role-id", Name = roleName };
        var options = new FrontendSecurityOptions();

        // Act
        var allowed = options.IsRoleAllowed(role);

        // Assert
        Assert.True(allowed);
    }

    [Fact]
    public void IsRoleAllowed_WhitelistNotConfigured_RejectsBackOfficeRole()
    {
        // Arrange
        var role = new Role { Id = "store-manager", Name = "Store manager" };
        var options = new FrontendSecurityOptions();

        // Act
        var allowed = options.IsRoleAllowed(role);

        // Assert
        Assert.False(allowed);
    }

    [Fact]
    public void IsRoleAllowed_WhitelistConfigured_ReplacesDefaultRoles()
    {
        // Arrange
        var role = new Role { Id = "role-id", Name = "Organization employee" };
        var options = new FrontendSecurityOptions { MembershipRolesWhitelist = ["Custom role"] };

        // Act
        var allowed = options.IsRoleAllowed(role);

        // Assert
        Assert.False(allowed);
    }

    [Fact]
    public void IsRoleAllowed_WhitelistContainsRoleId_AllowsRoleIgnoringCase()
    {
        // Arrange
        var role = new Role { Id = "custom-role-id", Name = "Custom role" };
        var options = new FrontendSecurityOptions { MembershipRolesWhitelist = ["CUSTOM-ROLE-ID"] };

        // Act
        var allowed = options.IsRoleAllowed(role);

        // Assert
        Assert.True(allowed);
    }
}
