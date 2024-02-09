using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using VirtoCommerce.CustomerModule.Core.Model;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Platform.Core;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Organization;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Vendor;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Authorization
{
    public class ProfileAuthorizationHandler : AuthorizationHandler<ProfileAuthorizationRequirement>
    {
        private readonly IMemberService _memberService;
        private readonly Func<UserManager<ApplicationUser>> _userManagerFactory;


        public ProfileAuthorizationHandler(IMemberService memberService, Func<UserManager<ApplicationUser>> userManagerFactory)
        {
            _memberService = memberService;
            _userManagerFactory = userManagerFactory;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ProfileAuthorizationRequirement requirement)
        {
            var result = context.User.IsInRole(PlatformConstants.Security.SystemRoles.Administrator);

            // Administrators can do anything except creating any users
            if (result && context.Resource is not CreateUserCommand)
            {
                context.Succeed(requirement);
                return;
            }

            using var userManager = _userManagerFactory();

            var currentUserId = GetUserId(context);
            var currentMember = await GetCustomerAsync(currentUserId, userManager);
            var currentContact = currentMember as Contact;

            switch (context.Resource)
            {
                // PT-6083: reduce complexity
                case ContactAggregate contactAggregate when currentContact != null:
                    result = currentContact.Id == contactAggregate.Contact.Id;
                    result = result || await HasSameOrganizationAsync(currentContact, contactAggregate.Contact.Id, userManager);
                    break;
                case ApplicationUser applicationUser:
                    result = currentUserId == applicationUser.Id;
                    result = result || await HasSameOrganizationAsync(currentContact, applicationUser.Id, userManager);
                    break;
                case OrganizationAggregate organizationAggregate when currentContact != null:
                    result = currentContact.Organizations.Contains(organizationAggregate.Organization.Id);
                    break;
                case VendorAggregate:
                    result = true;
                    break;
                case Role:
                    //Can be checked only with platform permission
                    result = true;
                    break;
                case CreateContactCommand:
                    //Anonymous user can create contact
                    result = true;
                    break;
                case CreateOrganizationCommand:
                    //New user can create organization on b2b-theme
                    result = true;
                    break;
                case CreateUserCommand createUserCommand:
                    //Anonymous user can create customer users only
                    result = !createUserCommand.ApplicationUser.IsAdministrator && createUserCommand.ApplicationUser.UserType.EqualsInvariant("Customer");
                    break;
                case SendVerifyEmailCommand:
                    //Anonymous user request verification email
                    result = true;
                    break;
                case DeleteContactCommand deleteContactCommand when currentContact != null:
                    result = await HasSameOrganizationAsync(currentContact, deleteContactCommand.ContactId, userManager);
                    break;
                case DeleteUserCommand deleteUserCommand when currentContact != null:
                    var allowDelete = true;
                    foreach (var userName in deleteUserCommand.UserNames)
                    {
                        if (allowDelete)
                        {
                            var user = await userManager.FindByNameAsync(userName);
                            allowDelete = await HasSameOrganizationAsync(currentContact, user?.MemberId, userManager);
                        }
                    }
                    result = allowDelete;
                    break;
                case MemberCommand memberCommand:
                    result = memberCommand.MemberId == currentMember?.Id;
                    if (!result && currentContact != null)
                    {
                        var memberId = memberCommand.MemberId;
                        var member = await _memberService.GetByIdAsync(memberId);
                        if (member.MemberType.EqualsInvariant("Organization") && currentContact.Organizations.Any(x => x.EqualsInvariant(member.Id)))
                        {
                            result = true;
                        }
                        else
                        {
                            result = await HasSameOrganizationAsync(currentContact, memberId, userManager);
                        }
                    }
                    break;
                case UpdateContactCommand updateContactCommand when currentContact != null:
                    result = updateContactCommand.Id == currentContact.Id;
                    result = result || await HasSameOrganizationAsync(currentContact, updateContactCommand.Id, userManager);
                    break;
                case UpdateOrganizationCommand updateOrganizationCommand when currentContact != null:
                    result = currentContact.Organizations.Contains(updateOrganizationCommand.Id);
                    break;
                case UpdateMemberDynamicPropertiesCommand:
                    //Can be checked only with platform permission
                    result = true;
                    break;
                case UpdateRoleCommand:
                    //Can be checked only with platform permission
                    result = true;
                    break;
                case UpdateUserCommand updateUserCommand when currentContact != null:
                    result = updateUserCommand.ApplicationUser.Id == currentContact.Id;
                    result = result || await HasSameOrganizationAsync(currentContact, updateUserCommand.ApplicationUser.Id, userManager);
                    break;
                case UpdatePersonalDataCommand updatePersonalDataCommand:
                    updatePersonalDataCommand.UserId = currentUserId;
                    result = true;
                    break;
                case InviteUserCommand inviteUserCommand:
                    var currentUser = await userManager.FindByIdAsync(currentUserId);
                    if (!string.IsNullOrEmpty(inviteUserCommand.OrganizationId) && currentContact != null && currentUser != null)
                    {
                        result = currentContact.Organizations.Contains(inviteUserCommand.OrganizationId)
                                 && currentUser.StoreId.EqualsInvariant(inviteUserCommand.StoreId);
                    }
                    else if (currentUser != null)
                    {
                        result = currentUser.StoreId.EqualsInvariant(inviteUserCommand.StoreId);
                    }
                    break;
                case LockOrganizationContactCommand lockOrganizationContact:
                    result = await HasSameOrganizationAsync(currentContact, lockOrganizationContact.UserId, userManager);
                    break;
                case UnlockOrganizationContactCommand unlockOrganizationContact:
                    result = await HasSameOrganizationAsync(currentContact, unlockOrganizationContact.UserId, userManager);
                    break;
                case ChangeOrganizationContactRoleCommand changeOrganizationContactRoleCommand:
                    result = await HasSameOrganizationAsync(currentContact, changeOrganizationContactRoleCommand.UserId, userManager);
                    break;
                case RemoveMemberFromOrganizationCommand removeMemberFromOrganizationCommand:
                    result = await HasSameOrganizationAsync(currentContact, removeMemberFromOrganizationCommand.ContactId, userManager);
                    break;
                case ChangePasswordCommand changePasswordCommand:
                    result = changePasswordCommand.UserId == currentUserId;
                    break;
                case AddAddressToFavoritesCommand command:
                    result = await CanAccessAddressAsync(currentContact, command.AddressId);
                    break;
                case RemoveAddressFromFavoritesCommand:
                    // Can remove any address from favorites if user is not anonymous
                    result = currentContact != null;
                    break;
            }

            if (result)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
        }

        private static string GetUserId(AuthorizationHandlerContext context)
        {
            //PT-5375 use ClaimTypes instead of "name"
            return context.User.FindFirstValue("name");
        }

        private async Task<bool> HasSameOrganizationAsync(Contact currentContact, string contactId, UserManager<ApplicationUser> userManager)
        {
            if (currentContact is null)
            {
                return false;
            }

            var contact = await GetCustomerAsync(contactId, userManager) as Contact;
            return currentContact.Organizations.Intersect(contact?.Organizations ?? Array.Empty<string>()).Any();
        }

        protected virtual async Task<Member> GetCustomerAsync(string customerId, UserManager<ApplicationUser> userManager)
        {
            if (string.IsNullOrWhiteSpace(customerId))
            {
                return null;
            }

            var result = await _memberService.GetByIdAsync(customerId);

            if (result == null)
            {
                var user = await userManager.FindByIdAsync(customerId);

                if (user?.MemberId != null)
                {
                    result = await _memberService.GetByIdAsync(user.MemberId);
                }
            }

            return result;
        }

        protected virtual async Task<bool> CanAccessAddressAsync(Contact contact, string addressId)
        {
            if (contact is null)
            {
                return false;
            }

            if (contact.Addresses != null && contact.Addresses.Any(x => x.Key == addressId))
            {
                return true;
            }

            var organizationId = contact.Organizations?.FirstOrDefault();
            if (string.IsNullOrEmpty(organizationId))
            {
                return false;
            }

            var organization = await _memberService.GetByIdAsync(organizationId);

            return
                organization != null &&
                organization.Addresses != null &&
                organization.Addresses.Any(x => x.Key == addressId);
        }
    }
}
