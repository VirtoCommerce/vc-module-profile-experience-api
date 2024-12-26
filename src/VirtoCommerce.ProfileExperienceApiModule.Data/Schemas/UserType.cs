using System;
using System.Linq;
using GraphQL;
using GraphQL.Resolvers;
using GraphQL.Types;
using MediatR;
using Microsoft.Extensions.Options;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Security.Extensions;
using VirtoCommerce.ProfileExperienceApiModule.Data.Aggregates.Contact;
using VirtoCommerce.ProfileExperienceApiModule.Data.Queries;
using VirtoCommerce.Xapi.Core.Extensions;
using VirtoCommerce.Xapi.Core.Helpers;
using VirtoCommerce.Xapi.Core.Schemas;
using VirtoCommerce.Xapi.Core.Services;

namespace VirtoCommerce.ProfileExperienceApiModule.Data.Schemas
{
    public class UserType : ExtendableGraphType<ApplicationUser>
    {
        public UserType(IContactAggregateRepository contactAggregateRepository, IUserManagerCore userManagerCore, IMediator mediator, IOptions<UserOptionsExtended> userOptionsExtended)
        {
            Field(x => x.AccessFailedCount);
            Field(x => x.CreatedBy, true);
            Field(x => x.CreatedDate, true);
            Field(x => x.Email, true);
            Field(x => x.EmailConfirmed);
            Field(x => x.Id);
            Field(x => x.IsAdministrator);
            Field(x => x.LockoutEnabled);
            Field<DateTimeGraphType>("lockoutEnd", resolve: x => x.Source.LockoutEnd);
            Field(x => x.MemberId, true);
            Field(x => x.ModifiedBy, true);
            Field(x => x.ModifiedDate, true);
            Field(x => x.NormalizedEmail, true);
            Field(x => x.NormalizedUserName, true);
            Field(x => x.PhoneNumber, true);
            Field(x => x.PhoneNumberConfirmed);
            Field(x => x.PhotoUrl, true);
            Field<ListGraphType<RoleType>>("roles", resolve: x => x.Source.Roles);
            Field<ListGraphType<StringGraphType>>("permissions", resolve: x => x.Source.Roles?.SelectMany(r => r.Permissions?.Select(p => p.Name)).Distinct(), description: "Account permissions");
            Field(x => x.SecurityStamp);
            Field(x => x.StoreId, true);
            Field(x => x.TwoFactorEnabled);
            Field(x => x.UserName);
            Field(x => x.UserType, true);

            Field<NonNullGraphType<BooleanGraphType>>("passwordExpired", resolve: x => GetPasswordExpired(x));
            Field<BooleanGraphType>("forcePasswordChange", resolve: x => GetPasswordExpired(x), description: "Make this user change their password when they sign in next time");
            Field<IntGraphType>("passwordExpiryInDays", resolve: x => GetPasswordExpiryInDays(x, userOptionsExtended.Value), description: "Password expiry in days");


            AddField(new FieldType
            {
                Name = "Contact",
                Description = "The associated contact info",
                Type = GraphTypeExtensionHelper.GetActualType<ContactType>(),
                Resolver = new FuncFieldResolver<ApplicationUser, ContactAggregate>(async context =>
                {
                    // It's possible to create a user without a contact since MemberId is nullable.
                    // Platform system users (frontend, admin, etc) usually don't have a contact.
                    if (context.Source.MemberId == null)
                    {
                        return null;
                    }

                    return await contactAggregateRepository.GetMemberAggregateRootByIdAsync<ContactAggregate>(context.Source.MemberId);
                }),
            });

            AddField(new FieldType
            {
                Name = "LockedState",
                Description = "Account locked state",
                Type = typeof(BooleanGraphType),
                Resolver = new FuncFieldResolver<ApplicationUser, bool>(async context => await userManagerCore.IsLockedOutAsync(context.Source)),
            });

            AddField(new FieldType
            {
                Name = "operator",
                Type = GraphTypeExtensionHelper.GetActualType<UserType>(),
                Resolver = new FuncFieldResolver<object>(async context =>
                {
                    if (context.UserContext.TryGetValue("OperatorUserName", out var operatorUser))
                    {
                        var result = await mediator.Send(new GetUserQuery
                        {
                            UserName = operatorUser as string
                        });
                        return result;
                    }

                    return null;
                })
            });
        }

        private static bool GetPasswordExpired(IResolveFieldContext<ApplicationUser> context)
        {
            return context.Source.PasswordExpired && !IsExternalSignIn(context);
        }

        private static int? GetPasswordExpiryInDays(IResolveFieldContext<ApplicationUser> context, UserOptionsExtended userOptionsExtended)
        {
            var result = (int?)null;

            var user = context.Source;

            if (!user.PasswordExpired &&
                !IsExternalSignIn(context) &&
                userOptionsExtended.RemindPasswordExpiryInDays > 0 &&
                userOptionsExtended.MaxPasswordAge != null &&
                userOptionsExtended.MaxPasswordAge.Value > TimeSpan.Zero)
            {
                var lastPasswordChangeDate = user.LastPasswordChangedDate ?? user.CreatedDate;
                var timeTillExpiry = lastPasswordChangeDate.Add(userOptionsExtended.MaxPasswordAge.Value) - DateTime.UtcNow;

                if (timeTillExpiry > TimeSpan.Zero &&
                    timeTillExpiry < TimeSpan.FromDays(userOptionsExtended.RemindPasswordExpiryInDays))
                {
                    result = timeTillExpiry.Days;
                }
            }

            return result;
        }

        private static bool IsExternalSignIn(IResolveFieldContext<ApplicationUser> context)
        {
            return context.Source.Id == context.GetCurrentUserId() && context.GetCurrentPrincipal().IsExternalSignIn();
        }
    }
}
