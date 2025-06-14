using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Builders;
using GraphQL.Resolvers;
using GraphQL.Types;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using VirtoCommerce.CoreModule.Core.Common;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Security.Authorization;
using VirtoCommerce.Platform.Security.Extensions;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Organization;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Vendor;
using VirtoCommerce.ProfileExperienceApiModule.Data.Authorization;
using VirtoCommerce.ProfileExperienceApiModule.Data.Commands;
using VirtoCommerce.ProfileExperienceApiModule.Data.Extensions;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models;
using VirtoCommerce.ProfileExperienceApiModule.Data.Models.RegisterOrganization;
using VirtoCommerce.ProfileExperienceApiModule.Data.Queries;
using VirtoCommerce.ProfileExperienceApiModule.Data.Schemas.RegisterCompany;
using VirtoCommerce.ProfileExperienceApiModule.Data.Services;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Helpers;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.Xapi.Core.Security.Authorization;
using CustomerPermissions = VirtoCommerce.CustomerModule.Core.ModuleConstants.Security.Permissions;
using PlatformPermissions = VirtoCommerce.Platform.Core.PlatformConstants.Security.Permissions;
using ProfilePermissions = VirtoCommerce.ProfileExperienceApiModule.Data.ModuleConstants.Security.Permissions;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class ProfileSchema : ISchemaBuilder, IProfileAuthorizationService
    {
        public const string _commandName = "command";

        private readonly IMediator _mediator;
        private readonly IAuthorizationService _authorizationService;
        private readonly Func<SignInManager<ApplicationUser>> _signInManagerFactory;
        private readonly IMemberAggregateFactory _factory;
        private readonly ILogger<ProfileSchema> _logger;

        public ProfileSchema(
            IMediator mediator,
            IAuthorizationService authorizationService,
            Func<SignInManager<ApplicationUser>> signInManagerFactory,
            IMemberAggregateFactory factory,
            ILogger<ProfileSchema> logger)
        {
            _mediator = mediator;
            _authorizationService = authorizationService;
            _signInManagerFactory = signInManagerFactory;
            _factory = factory;
            _logger = logger;
        }

        public void Build(ISchema schema)
        {
            // Value converters
            ValueConverter.Register<int, AddressType>(x => (AddressType)x);

            //Queries

            schema.Query.AddField(new FieldType
            {
                Name = "me",
                Arguments = new QueryArguments(new QueryArgument<StringGraphType> { Name = "userId" }),
                Type = GraphTypeExtensionHelper.GetActualType<UserType>(),
                Resolver = new FuncFieldResolver<object>(async context =>
                {
                    var principal = context.GetCurrentPrincipal();
                    var userName = principal?.Identity?.Name;
                    if (!string.IsNullOrEmpty(userName))
                    {
                        var result = await _mediator.Send(new GetUserQuery
                        {
                            UserName = userName
                        });
                        return result;
                    }

                    var anonymousUser = AnonymousUser.Instance;

                    var userId = principal?.FindFirstValue(ClaimTypes.NameIdentifier) ??
                                 context.GetArgument<string>("userId");

                    if (!string.IsNullOrEmpty(userId) && !await UserExistsAsync(userId))
                    {
                        anonymousUser.Id = userId;
                    }

                    return anonymousUser;
                })
            });

            schema.AddMemberQuery<OrganizationAggregate, OrganizationType, GetOrganizationByIdQuery>(
                _mediator, "organization", (context, aggregate) => CheckAuthAsync(context, aggregate));

            schema.AddMemberQuery<ContactAggregate, ContactType, GetContactByIdQuery>(
                _mediator, "contact", (context, aggregate) => CheckAuthAsync(context, aggregate));

            schema.AddMemberQuery<VendorAggregate, VendorType, GetVendorByIdQuery>(
                _mediator, "vendor", (context, aggregate) => CheckAuthAsync(context, aggregate));

            var organizationsConnectionBuilder = GraphTypeExtensionHelper
                .CreateConnection<OrganizationType, object>("organizations")
                .Argument<StringGraphType>("searchPhrase", "This parameter applies a filter to the query results")
                .Argument<StringGraphType>("sort", "The sort expression")
                .PageSize(20);

            organizationsConnectionBuilder.ResolveAsync(async context =>
            {
                context.CopyArgumentsToUserContext();

                await CheckAuthAsync(context, context);

                var query = context.GetSearchMembersQuery<SearchOrganizationsQuery>();
                query.DeepSearch = true;

                var response = await _mediator.Send(query);

                return new PagedConnection<OrganizationAggregate>(response.Results.Select(x => _factory.Create<OrganizationAggregate>(x)), query.Skip, query.Take, response.TotalCount);
            });

            schema.Query.AddField(organizationsConnectionBuilder.FieldType);

            var contactsConnectionBuilder = GraphTypeExtensionHelper
                .CreateConnection<ContactType, object>("contacts")
                .Argument<StringGraphType>("searchPhrase", "This parameter applies a filter to the query results")
                .Argument<StringGraphType>("sort", "The sort expression")
                .PageSize(20);

            contactsConnectionBuilder.ResolveAsync(async context =>
            {
                context.CopyArgumentsToUserContext();

                await CheckAuthAsync(context, context);

                var query = context.GetSearchMembersQuery<SearchContactsQuery>();
                query.DeepSearch = true;

                var response = await _mediator.Send(query);

                return new PagedConnection<ContactAggregate>(response.Results.Select(x => _factory.Create<ContactAggregate>(x)), query.Skip, query.Take, response.TotalCount);
            });

            schema.Query.AddField(contactsConnectionBuilder.FieldType);

#pragma warning disable S125 // Sections of code should not be commented out
            /*                         
               query {
                     checkUsernameUniqueness(username: "testUser")
               }                         
            */
#pragma warning restore S125 // Sections of code should not be commented out

            _ = schema.Query.AddField(new FieldType
            {
                Name = "checkUsernameUniqueness",
                Arguments = new QueryArguments(new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "username" }),
                Type = GraphTypeExtensionHelper.GetActualType<BooleanGraphType>(),
                Resolver = new FuncFieldResolver<object>(async context =>
                {
                    var result = await _mediator.Send(new CheckUsernameUniquenessQuery
                    {
                        Username = context.GetArgument<string>("username"),
                    });

                    return result.IsUnique;
                })
            });

#pragma warning disable S125 // Sections of code should not be commented out
            /*                         
               query {
                     checkEmailUniqueness(email: "user@email")
               }                         
            */
#pragma warning restore S125 // Sections of code should not be commented out

            _ = schema.Query.AddField(new FieldType
            {
                Name = "checkEmailUniqueness",
                Arguments = new QueryArguments(new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "email" }),
                Type = GraphTypeExtensionHelper.GetActualType<BooleanGraphType>(),
                Resolver = new FuncFieldResolver<object>(async context =>
                {
                    var result = await _mediator.Send(new CheckEmailUniquenessQuery
                    {
                        Email = context.GetArgument<string>("email"),
                    });

                    return result.IsUnique;
                })
            });

#pragma warning disable S125 // Sections of code should not be commented out
            /*                         
               query {
                     requestPasswordReset(loginOrEmail: "user@email")
               }                         
            */
#pragma warning restore S125 // Sections of code should not be commented out

            _ = schema.Query.AddField(new FieldType
            {
                Name = "requestPasswordReset",
                Arguments = new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "loginOrEmail" },
                    new QueryArgument<StringGraphType> { Name = "urlSuffix" }),
                Type = GraphTypeExtensionHelper.GetActualType<BooleanGraphType>(),
                Resolver = new FuncFieldResolver<object>(async context =>
                {
                    var result = await _mediator.Send(new RequestPasswordResetQuery
                    {
                        LoginOrEmail = context.GetArgument<string>("loginOrEmail"),
                        UrlSuffix = context.GetArgument<string>("urlSuffix"),
                    });

                    return result;
                })
            });

#pragma warning disable S125 // Sections of code should not be commented out
            /*                         
               query {
                     validatePassword(password: "pswd")
               }                         
            */
#pragma warning restore S125 // Sections of code should not be commented out
            _ = schema.Query.AddField(new FieldType
            {
                Name = "validatePassword",
                Arguments = new QueryArguments(new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "password" }),
                Type = GraphTypeExtensionHelper.GetActualType<CustomIdentityResultType>(),
                Resolver = new FuncFieldResolver<object>(async context =>
                {
                    var result = await _mediator.Send(new PasswordValidationQuery
                    {
                        Password = context.GetArgument<string>("password"),
                    });

                    return result;
                })
            });

            #region updateAddressMutation

            // sample code for updating addresses:
#pragma warning disable S125 // Sections of code should not be commented out
            /*
                        mutation updateMemberAddresses($command: UpdateMemberAddressesCommand!){
                          updateMemberAddresses(command: $command)
                          {
                            memberType
                            addresses { key city countryCode countryName email firstName  lastName line1 line2 middleName name phone postalCode regionId regionName zip }
                          }
                        }
                        query variables:
                        {
                            "command": {
                              "memberId": "any-member-id",
                              "addresses": [{"addressType": "Shipping", "name": "string", "countryCode": "string", "countryName": "string", "city": "string", "postalCode": "string", "line1": "string", "regionId": "string", "regionName": "string", "firstName": "string", "lastName": "string", "phone": "string", "email": "string", "regionId": "string"
                                }]
                            }
                        }
                         */
#pragma warning restore S125 // Sections of code should not be commented out

            #endregion
            _ = schema.Mutation.AddField(FieldBuilder<object, MemberAggregateRootBase>
                            .Create("updateMemberAddresses", GraphTypeExtensionHelper.GetActualType<MemberType>())
                            .Argument(GraphTypeExtensionHelper.GetActualComplexType<NonNullGraphType<InputUpdateMemberAddressType>>(), _commandName)
                            .ResolveAsync(async context =>
                            {
                                var type = GenericTypeHelper.GetActualType<UpdateMemberAddressesCommand>();
                                var command = (UpdateMemberAddressesCommand)context.GetArgument(type, _commandName);
                                await CheckAuthAsync(context, command);
                                return await _mediator.Send(command);
                            })
                            .FieldType);

            _ = schema.Mutation.AddField(FieldBuilder<object, MemberAggregateRootBase>
                            .Create("deleteMemberAddresses", GraphTypeExtensionHelper.GetActualType<MemberType>())
                            .Argument(GraphTypeExtensionHelper.GetActualComplexType<NonNullGraphType<InputDeleteMemberAddressType>>(), _commandName)
                            .ResolveAsync(async context =>
                            {
                                var type = GenericTypeHelper.GetActualType<DeleteMemberAddressesCommand>();
                                var command = (DeleteMemberAddressesCommand)context.GetArgument(type, _commandName);
                                await CheckAuthAsync(context, command);
                                return await _mediator.Send(command);
                            })
                            .FieldType);

            _ = schema.Mutation.AddField(FieldBuilder<OrganizationAggregate, OrganizationAggregate>
                            .Create("updateOrganization", GraphTypeExtensionHelper.GetActualType<OrganizationType>())
                            .Argument(GraphTypeExtensionHelper.GetActualComplexType<NonNullGraphType<InputUpdateOrganizationType>>(), _commandName)
                            .ResolveAsync(async context =>
                            {
                                var type = GenericTypeHelper.GetActualType<UpdateOrganizationCommand>();
                                var command = (UpdateOrganizationCommand)context.GetArgument(type, _commandName);
                                await CheckAuthAsync(context, command, ProfilePermissions.MyOrganizationEdit);
                                return await _mediator.Send(command);
                            })
                            .FieldType);

            _ = schema.Mutation.AddField(FieldBuilder<OrganizationAggregate, OrganizationAggregate>
                            .Create("createOrganization", GraphTypeExtensionHelper.GetActualType<OrganizationType>())
                            .Argument(GraphTypeExtensionHelper.GetActualComplexType<NonNullGraphType<InputCreateOrganizationType>>(), _commandName)
                            .ResolveAsync(async context =>
                            {
                                var type = GenericTypeHelper.GetActualType<CreateOrganizationCommand>();
                                var command = (CreateOrganizationCommand)context.GetArgument(type, _commandName);
                                await CheckAuthAsync(context, command);
                                return await _mediator.Send(command);
                            })
                            .FieldType);

            _ = schema.Mutation.AddField(FieldBuilder<object, MemberAggregateRootBase>
                            .Create("removeMemberFromOrganization", GraphTypeExtensionHelper.GetActualType<ContactType>())
                            .Argument(GraphTypeExtensionHelper.GetActualComplexType<NonNullGraphType<InputRemoveMemberFromOrganizationType>>(), _commandName)
                            .ResolveAsync(async context =>
                            {
                                var type = GenericTypeHelper.GetActualType<RemoveMemberFromOrganizationCommand>();
                                var command = (RemoveMemberFromOrganizationCommand)context.GetArgument(type, _commandName);
                                await CheckAuthAsync(context, command, ProfilePermissions.MyOrganizationEdit);
                                return await _mediator.Send(command);
                            })
                            .FieldType);

            _ = schema.Mutation.AddField(FieldBuilder<RegisterOrganizationResult, RegisterOrganizationResult>
                            .Create("requestRegistration", GraphTypeExtensionHelper.GetActualType<RequestRegistrationType>())
                            .Argument(GraphTypeExtensionHelper.GetActualComplexType<NonNullGraphType<InputRequestRegistrationType>>(), _commandName)
                            .ResolveAsync(async context =>
                            {
                                var type = GenericTypeHelper.GetActualType<RegisterRequestCommand>();
                                var command = (RegisterRequestCommand)context.GetArgument(type, _commandName);
                                return await _mediator.Send(command);
                            })
                            .FieldType);

            _ = schema.Mutation.AddField(FieldBuilder<ContactAggregate, ContactAggregate>
                            .Create("createContact", GraphTypeExtensionHelper.GetActualType<ContactType>())
                            .Argument(GraphTypeExtensionHelper.GetActualComplexType<NonNullGraphType<InputCreateContactType>>(), _commandName)
                            .ResolveAsync(async context =>
                            {
                                var type = GenericTypeHelper.GetActualType<CreateContactCommand>();
                                var command = (CreateContactCommand)context.GetArgument(type, _commandName);
                                await CheckAuthAsync(context, command);

                                return await _mediator.Send(command);
                            })
                            .FieldType);

            _ = schema.Mutation.AddField(FieldBuilder<ContactAggregate, ContactAggregate>
                            .Create("updateContact", GraphTypeExtensionHelper.GetActualType<ContactType>())
                            .Argument(GraphTypeExtensionHelper.GetActualComplexType<NonNullGraphType<InputUpdateContactType>>(), _commandName)
                            .ResolveAsync(async context =>
                            {
                                var type = GenericTypeHelper.GetActualType<UpdateContactCommand>();
                                var command = (UpdateContactCommand)context.GetArgument(type, _commandName);
                                command.UserId = context.GetCurrentUserId();
                                command.OrganizationId = context.GetCurrentOrganizationId();
                                await CheckAuthAsync(context, command);
                                return await _mediator.Send(command);

                            })
                            .FieldType);

            _ = schema.Mutation.AddField(FieldBuilder<ContactAggregate, bool>
                            .Create("deleteContact", typeof(BooleanGraphType))
                            .Argument(GraphTypeExtensionHelper.GetActualComplexType<NonNullGraphType<InputDeleteContactType>>(), _commandName)
                            .ResolveAsync(async context =>
                            {
                                var type = GenericTypeHelper.GetActualType<DeleteContactCommand>();
                                var command = (DeleteContactCommand)context.GetArgument(type, _commandName);
                                await CheckAuthAsync(context, command, CustomerPermissions.Delete);
                                return await _mediator.Send(command);
                            })
                            .FieldType);

            _ = schema.Mutation.AddField(FieldBuilder<object, IdentityResult>
                          .Create("updatePersonalData", GraphTypeExtensionHelper.GetActualType<IdentityResultType>())
                          .Argument(GraphTypeExtensionHelper.GetActualComplexType<NonNullGraphType<InputUpdatePersonalDataType>>(), _commandName)
                          .ResolveAsync(async context =>
                          {
                              var type = GenericTypeHelper.GetActualType<UpdatePersonalDataCommand>();
                              var command = (UpdatePersonalDataCommand)context.GetArgument(type, _commandName);
                              await CheckAuthAsync(context, command);
                              return await _mediator.Send(command);
                          })
                          .FieldType);

            _ = schema.Mutation.AddField(FieldBuilder<object, IMemberAggregateRoot>
                        .Create("updateMemberDynamicProperties", GraphTypeExtensionHelper.GetActualType<MemberType>())
                        .Argument(GraphTypeExtensionHelper.GetActualComplexType<NonNullGraphType<InputUpdateMemberDynamicPropertiesType>>(), _commandName)
                        .ResolveAsync(async context =>
                        {
                            var type = GenericTypeHelper.GetActualType<UpdateMemberDynamicPropertiesCommand>();
                            var command = (UpdateMemberDynamicPropertiesCommand)context.GetArgument(type, _commandName);
                            await CheckAuthAsync(context, command, CustomerPermissions.Update);

                            return await _mediator.Send(command);
                        })
                        .FieldType);

            _ = schema.Mutation.AddField(FieldBuilder<object, bool>
                        .Create("sendVerifyEmail", typeof(BooleanGraphType))
                        .Argument(GraphTypeExtensionHelper.GetActualType<InputSendVerifyEmailType>(), _commandName)
                        .ResolveAsync(async context =>
                        {
                            var type = GenericTypeHelper.GetActualType<SendVerifyEmailCommand>();
                            var command = (SendVerifyEmailCommand)context.GetArgument(type, _commandName);

                            if (context.IsAuthenticated())
                            {
                                command.UserId = context.GetCurrentUserId();
                                command.Email = await GetUserEmailAsync(context.GetCurrentUserId());
                            }

                            return await _mediator.Send(command);
                        })
                        .FieldType);

            _ = schema.Mutation.AddField(FieldBuilder<RegisterOrganizationResult, IdentityResultResponse>
                        .Create("confirmEmail", GraphTypeExtensionHelper.GetActualType<CustomIdentityResultType>())
                        .Argument(GraphTypeExtensionHelper.GetActualComplexType<NonNullGraphType<InputConfirmEmailType>>(), _commandName)
                        .ResolveAsync(async context =>
                        {
                            var type = GenericTypeHelper.GetActualType<ConfirmEmailCommand>();
                            var command = (ConfirmEmailCommand)context.GetArgument(type, _commandName);

                            return await _mediator.Send(command);
                        })
                        .FieldType);

            _ = schema.Mutation.AddField(FieldBuilder<object, IdentityResultResponse>
                        .Create("resetPasswordByToken", GraphTypeExtensionHelper.GetActualType<CustomIdentityResultType>())
                        .Argument(GraphTypeExtensionHelper.GetActualComplexType<InputResetPasswordByTokenType>(), _commandName)
                        .ResolveAsync(async context =>
                        {
                            var type = GenericTypeHelper.GetActualType<ResetPasswordByTokenCommand>();
                            var command = (ResetPasswordByTokenCommand)context.GetArgument(type, _commandName);

                            return await _mediator.Send(command);
                        })
                        .FieldType);

            _ = schema.Mutation.AddField(FieldBuilder<object, IdentityResultResponse>
                        .Create("changePassword", GraphTypeExtensionHelper.GetActualType<CustomIdentityResultType>())
                        .Argument(GraphTypeExtensionHelper.GetActualComplexType<InputChangePasswordType>(), _commandName)
                        .ResolveAsync(async context =>
                        {
                            var type = GenericTypeHelper.GetActualType<ChangePasswordCommand>();
                            var command = (ChangePasswordCommand)context.GetArgument(type, _commandName);
                            await CheckAuthAsync(context, command, checkPasswordExpired: false);

                            return await _mediator.Send(command);
                        })
                        .FieldType);

            _ = schema.Mutation.AddField(FieldBuilder<ContactAggregate, ContactAggregate>
                        .Create("lockOrganizationContact", GraphTypeExtensionHelper.GetActualType<ContactType>())
                        .Argument(GraphTypeExtensionHelper.GetActualComplexType<NonNullGraphType<InputLockUnlockOrganizationContactType>>(), _commandName)
                        .ResolveAsync(async context =>
                        {
                            var type = GenericTypeHelper.GetActualType<LockOrganizationContactCommand>();
                            var command = (LockOrganizationContactCommand)context.GetArgument(type, _commandName);
                            await CheckAuthAsync(context, command, ProfilePermissions.MyOrganizationEdit);
                            return await _mediator.Send(command);
                        })
                        .FieldType);

            _ = schema.Mutation.AddField(FieldBuilder<ContactAggregate, ContactAggregate>
                        .Create("unlockOrganizationContact", GraphTypeExtensionHelper.GetActualType<ContactType>())
                        .Argument(GraphTypeExtensionHelper.GetActualComplexType<NonNullGraphType<InputLockUnlockOrganizationContactType>>(), _commandName)
                        .ResolveAsync(async context =>
                        {
                            var type = GenericTypeHelper.GetActualType<UnlockOrganizationContactCommand>();
                            var command = (UnlockOrganizationContactCommand)context.GetArgument(type, _commandName);
                            await CheckAuthAsync(context, command, ProfilePermissions.MyOrganizationEdit);
                            return await _mediator.Send(command);
                        })
                        .FieldType);

            // Security API fields

            #region user query

#pragma warning disable S125 // Sections of code should not be commented out
            /*
                            {
                                user(id: "1eb2fa8ac6574541afdb525833dadb46"){
                                userName isAdministrator roles { name } userType memberId storeId
                                }
                            }
                         */
#pragma warning restore S125 // Sections of code should not be commented out

            #endregion
            _ = schema.Query.AddField(new FieldType
            {
                Name = "user",
                Arguments = new QueryArguments(
                    new QueryArgument<StringGraphType> { Name = "id" },
                    new QueryArgument<StringGraphType> { Name = "userName" },
                    new QueryArgument<StringGraphType> { Name = "email" },
                    new QueryArgument<StringGraphType> { Name = "loginProvider" },
                    new QueryArgument<StringGraphType> { Name = "providerKey" }),
                Type = GraphTypeExtensionHelper.GetActualType<UserType>(),
                Resolver = new FuncFieldResolver<object>(async context =>
                {
                    var user = await _mediator.Send(new GetUserQuery(
                        id: context.GetArgument<string>("id"),
                        email: context.GetArgument<string>("email"),
                        userName: context.GetArgument<string>("userName"),
                        loginProvider: context.GetArgument<string>("loginProvider"),
                        providerKey: context.GetArgument<string>("providerKey")));

                    await CheckAuthAsync(context, user);

                    return user;
                })
            });

            #region role query

#pragma warning disable S125 // Sections of code should not be commented out
            /*
                         {
                          getRole(roleName: "Use api"){
                           permissions
                          }
                        }
                         */
#pragma warning restore S125 // Sections of code should not be commented out

            #endregion
            _ = schema.Query.AddField(new FieldType
            {
                Name = "role",
                Arguments = new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "roleName" }
                ),
                Type = GraphTypeExtensionHelper.GetActualType<RoleType>(),
                Resolver = new FuncFieldResolver<object>(async context =>
                {
                    var result = await _mediator.Send(new GetRoleQuery(context.GetArgument<string>("roleName")));

                    return result;
                })
            });

            #region invite user

#pragma warning disable S125 // Sections of code should not be commented out
            /*
            mutation ($command: InputInviteUserType!){
                inviteUser(command: $command){ succeeded errors { code }}
            }
            Query variables:
            {
                "command": {
                    "storeId": "my-store",
                    "organizationId": "my-org",
                    "urlSuffix": "/invite",
                    "email": "example@example.org",
                    "message": "Message"
                }
            }
             */
#pragma warning restore S125 // Sections of code should not be commented out

            #endregion
            _ = schema.Mutation.AddField(
                        FieldBuilder<object, IdentityResultResponse>
                        .Create("inviteUser", GraphTypeExtensionHelper.GetActualType<CustomIdentityResultType>())
                        .Argument(GraphTypeExtensionHelper.GetActualComplexType<NonNullGraphType<InputInviteUserType>>(), _commandName)
                        .ResolveAsync(async context =>
                        {
                            var type = GenericTypeHelper.GetActualType<InviteUserCommand>();
                            var command = (InviteUserCommand)context.GetArgument(type, _commandName);
                            await CheckAuthAsync(context, command);
                            return await _mediator.Send(command);
                        })
                        .FieldType);

            #region register by invitation

#pragma warning disable S125 // Sections of code should not be commented out
            /*
            mutation ($command: InputRegisterByInvitationType!){
                registerByInvitation(command: $command){ succeeded errors { code }}
            }
            Query variables:
            {
                "command": {
                    "userId": "my-user",
                    "token": "large-unique-token",
                    "firstName": "John",
                    "lastName": "Smith",
                    "phone": "+12025550000",
                    "userName": "johnsmith",
                    "password": "password1!"
                }
            }
             */
#pragma warning restore S125 // Sections of code should not be commented out

            #endregion
            _ = schema.Mutation.AddField(FieldBuilder<object, IdentityResultResponse>
                        .Create("registerByInvitation", GraphTypeExtensionHelper.GetActualType<CustomIdentityResultType>())
                        .Argument(GraphTypeExtensionHelper.GetActualComplexType<NonNullGraphType<InputRegisterByInvitationType>>(), _commandName)
                        .ResolveAsync(async context =>
                        {
                            var type = GenericTypeHelper.GetActualType<RegisterByInvitationCommand>();
                            var command = (RegisterByInvitationCommand)context.GetArgument(type, _commandName);
                            return await _mediator.Send(command);
                        })
                        .FieldType);

            #region create user

#pragma warning disable S125 // Sections of code should not be commented out
            /*
            mutation ($command: InputCreateUserType!){
                createUser(command: $command){ succeeded errors { code }}
            }
            Query variables:
            {
                "command": {
                "createdBy": "eXp1", "email": "eXp1@mail.com", "password":"eXp1@mail.com", "userName": "eXp1@mail.com", "userType": "Customer"
                }
            }
             */
#pragma warning restore S125 // Sections of code should not be commented out

            #endregion
            _ = schema.Mutation.AddField(FieldBuilder<object, IdentityResult>
                        .Create("createUser", GraphTypeExtensionHelper.GetActualType<IdentityResultType>())
                        .Argument(GraphTypeExtensionHelper.GetActualComplexType<NonNullGraphType<InputCreateUserType>>(), _commandName)
                        .ResolveAsync(async context =>
                        {
                            var type = GenericTypeHelper.GetActualType<CreateUserCommand>();
                            var command = (CreateUserCommand)context.GetArgument(type, _commandName);
                            await CheckAuthAsync(context, command);
                            return await _mediator.Send(command);
                        })
                        .FieldType);

            #region update user

#pragma warning disable S125 // Sections of code should not be commented out
            /*
                         mutation ($command: InputUpdateUserType!){
                          updateUser(command: $command){ succeeded errors { description } }
                        }
                        Query variables:
                        {
                         "command":{
                          "securityStamp": ...,
                          "userType": "Customer",
                          "roles": [],
                          "id": "b5d28a83-c296-4212-b89e-046fca3866be",
                          "userName": "_loGIN999",
                          "email": "_loGIN999@gmail.com"
                            }
                        }
                         */
#pragma warning restore S125 // Sections of code should not be commented out

            #endregion
            _ = schema.Mutation.AddField(FieldBuilder<object, IdentityResult>
                        .Create("updateUser", GraphTypeExtensionHelper.GetActualType<IdentityResultType>())
                        .Argument(GraphTypeExtensionHelper.GetActualComplexType<NonNullGraphType<InputUpdateUserType>>(), _commandName)
                        .ResolveAsync(async context =>
                        {
                            var type = GenericTypeHelper.GetActualType<UpdateUserCommand>();
                            var command = (UpdateUserCommand)context.GetArgument(type, _commandName);
                            await CheckAuthAsync(context, command);
                            return await _mediator.Send(command);
                        })
                        .FieldType);

            _ = schema.Mutation.AddField(FieldBuilder<object, IdentityResultResponse>
                        .Create("changeOrganizationContactRole", GraphTypeExtensionHelper.GetActualType<CustomIdentityResultType>())
                        .Argument(GraphTypeExtensionHelper.GetActualComplexType<NonNullGraphType<InputChangeOrganizationContactRoleType>>(), _commandName)
                        .ResolveAsync(async context =>
                        {
                            var type = GenericTypeHelper.GetActualType<ChangeOrganizationContactRoleCommand>();
                            var command = (ChangeOrganizationContactRoleCommand)context.GetArgument(type, _commandName);
                            await CheckAuthAsync(context, command, ProfilePermissions.MyOrganizationEdit);
                            return await _mediator.Send(command);
                        })
            .FieldType);

            #region delete user

#pragma warning disable S125 // Sections of code should not be commented out
            /*
             mutation ($command: InputDeleteUserType!){
              deleteUser(command: $command){ succeeded errors { description } }
            }
            Query variables:
            {
              "command": {
                "userNames": ["admin",  "eXp1@mail.com"]
              }
            }
             */
#pragma warning restore S125 // Sections of code should not be commented out

            #endregion
            _ = schema.Mutation.AddField(FieldBuilder<object, IdentityResult>
                        .Create("deleteUsers", GraphTypeExtensionHelper.GetActualType<IdentityResultType>())
                        .Argument(GraphTypeExtensionHelper.GetActualComplexType<NonNullGraphType<InputDeleteUserType>>(), _commandName)
                        .ResolveAsync(async context =>
                        {
                            var type = GenericTypeHelper.GetActualType<DeleteUserCommand>();
                            var command = (DeleteUserCommand)context.GetArgument(type, _commandName);
                            await CheckAuthAsync(context, command, PlatformPermissions.SecurityDelete);
                            return await _mediator.Send(command);
                        })
                        .FieldType);

            #region update role query

#pragma warning disable S125 // Sections of code should not be commented out
            /*
                         mutation ($command: InputUpdateRoleType!){
                          updateRole(command: $command){ succeeded errors { description } }
                        }
                        Query variables:
                        {
                         "command":{
                         "id": "graphtest",  "name": "graphtest", "permissions": [
                            { "name": "order:read", "assignedScopes": [{"scope": "{{userId}}", "type": "OnlyOrderResponsibleScope" }] }
                          ]
                         }
                        }
                         */
#pragma warning restore S125 // Sections of code should not be commented out

            #endregion
            _ = schema.Mutation.AddField(FieldBuilder<object, IdentityResult>
                        .Create("updateRole", GraphTypeExtensionHelper.GetActualType<IdentityResultType>())
                        .Argument(GraphTypeExtensionHelper.GetActualComplexType<NonNullGraphType<InputUpdateRoleType>>(), _commandName)
                        .ResolveAsync(async context =>
                        {
                            var type = GenericTypeHelper.GetActualType<UpdateRoleCommand>();
                            var command = (UpdateRoleCommand)context.GetArgument(type, _commandName);
                            await CheckAuthAsync(context, command, PlatformPermissions.SecurityUpdate);

                            return await _mediator.Send(command);
                        })
                        .FieldType);
        }

        // PT-1654: Fix Authentication
        public async Task CheckAuthAsync(IResolveFieldContext context, object resource, string permission = null, bool checkPasswordExpired = true)
        {
            var principal = context.GetCurrentPrincipal();
            var userId = principal.GetCurrentUserId();
            var isExternalSignIn = principal.IsExternalSignIn();
            var signInManager = _signInManagerFactory();

            try
            {
                var user = await signInManager.UserManager.FindByIdAsync(userId) ?? new ApplicationUser
                {
                    Id = userId,
                    UserName = Xapi.Core.ModuleConstants.AnonymousUser.UserName,
                };

                if (checkPasswordExpired && user.PasswordExpired && !isExternalSignIn)
                {
                    throw AuthorizationError.PasswordExpired();
                }

                // Why do we create a new principal???
                var userPrincipal = await signInManager.CreateUserPrincipalAsync(user);

                if (!string.IsNullOrEmpty(permission) && PermissionRequired(user, resource))
                {
                    if (user.Logins is null)
                    {
                        throw AuthorizationError.AnonymousAccessDenied();
                    }

                    var permissionAuthorizationResult = await _authorizationService.AuthorizeAsync(userPrincipal,
                        null, new PermissionAuthorizationRequirement(permission));
                    if (!permissionAuthorizationResult.Succeeded)
                    {
                        throw AuthorizationError.PermissionRequired(permission);
                    }
                }

                var authorizationResult = await _authorizationService.AuthorizeAsync(userPrincipal, resource,
                    new ProfileAuthorizationRequirement());

                if (!authorizationResult.Succeeded)
                {
                    throw AuthorizationError.Forbidden();
                }
            }
            catch (AuthorizationError ex)
            {
                _logger.Log(LogLevel.Error,
                    "message: {message}, userId: {userId}, resource: {resource}, permission: {permission}",
                    ex.Message, userId, resource, permission);
                throw;
            }
            finally
            {
                signInManager.UserManager.Dispose();
            }
        }

        private static bool PermissionRequired(ApplicationUser user, object resource)
        {
            if (resource is UpdateMemberDynamicPropertiesCommand updateMemberDynamicPropertiesCommand)
            {
                return updateMemberDynamicPropertiesCommand.MemberId != user.MemberId;
            }

            return true;
        }

        private async Task<string> GetUserEmailAsync(string userId)
        {
            var signInManager = _signInManagerFactory();

            var user = await signInManager.UserManager.FindByIdAsync(userId);

            return user?.Email;
        }

        private async Task<bool> UserExistsAsync(string userId)
        {
            var signInManager = _signInManagerFactory();
            var user = await signInManager.UserManager.FindByIdAsync(userId);

            return user != null;
        }
    }
}
