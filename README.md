# Virto Commerce Profile Experience API (xProfile) Module

The xProfile module provides high performance search queries for customer and organization data.

## Key features

Through xProfile, you can perform  Create, Read, Update, and Delete (CRUD) operations with **users**, **organizations**, and **contacts**.

## How It Works
Just like other xAPI modules, xProfile module resides on a website's or application's back end and is managed with queries. It enables efficiently creating, updating, and deleting various entity profiles. 

## Documentation

* [xProfile module documentation](https://docs.virtocommerce.org/platform/developer-guide/GraphQL-Storefront-API-Reference-xAPI/Profile/overview/)
* [View on GitHub](https://github.com/VirtoCommerce/vc-module-profile-experience-api)
* [Experience API Documentation](https://docs.virtocommerce.org/platform/developer-guide/GraphQL-Storefront-API-Reference-xAPI/)
* [Getting started](https://docs.virtocommerce.org/platform/developer-guide/GraphQL-Storefront-API-Reference-xAPI/getting-started/)
* [How to use GraphiQL](https://docs.virtocommerce.org/platform/developer-guide/GraphQL-Storefront-API-Reference-xAPI/graphiql/)
* [How to use Postman](https://docs.virtocommerce.org/platform/developer-guide/GraphQL-Storefront-API-Reference-xAPI/postman/)
* [How to extend](https://docs.virtocommerce.org/platform/developer-guide/GraphQL-Storefront-API-Reference-xAPI/x-api-extensions/)
* [Virto Commerce Frontend architecture](https://docs.virtocommerce.org/storefront/developer-guide/architecture/)

## Configuration

### Input Validation

The module includes server-side input validation to prevent stored XSS attacks. Validation is configurable via `appsettings.json` under `FrontendSecurity:InputValidation`:

```json
{
  "FrontendSecurity": {
    "InputValidation": {
      "NameValidationPattern": "^[\\p{L}\\p{M}\\s'\\-\\.]+$",
      "OrganizationNameValidationPattern": "^[\\p{L}\\p{M}\\p{N}\\s'\\-\\.&#/,()]+$",
      "EnableNoHtmlTagsValidation": true,
      "EnableScriptInjectionValidation": true
    }
  }
}
```

| Setting | Default | Description |
|---------|---------|-------------|
| `NameValidationPattern` | `^[\p{L}\p{M}\s'\-\.]+$` | Allow-list regex for person name fields (firstName, lastName, fullName). Permits Unicode letters, diacritics, spaces, apostrophes, hyphens, and dots. Set to empty string to disable. |
| `OrganizationNameValidationPattern` | `^[\p{L}\p{M}\p{N}\s'\-\.&#/,()]+$` | Allow-list regex for organization name fields. Additionally permits numbers, `&`, `#`, `/`, `,`, `(`, `)` for names like "3M", "AT&T", "H&M". Set to empty string to disable. |
| `EnableNoHtmlTagsValidation` | `true` | Rejects HTML tags (`<...>`) in non-name fields (username, phone, address lines, city). Set to `false` to disable. |
| `EnableScriptInjectionValidation` | `true` | Rejects script injection patterns (`<script>`, `javascript:`, `vbscript:`, `data:text/html`) in free-text fields (description). Set to `false` to disable. |

> MaxLength constraints are always enforced regardless of configuration.

#### Validation errors

`createContact`, `updateContact`, `updateMemberAddresses`, `createOrganization`, `updateOrganization` and `updatePersonalData` report rejected input as GraphQL errors, one per failed rule. The mutation returns `null`, and each error keeps the `VALIDATION` code and names the input field and the rule:

```json
{
  "errors": [
    {
      "message": "'Last Name' contains invalid characters.",
      "locations": [{ "line": 2, "column": 3 }],
      "path": ["updateContact"],
      "extensions": {
        "code": "VALIDATION",
        "codes": ["VALIDATION"],
        "field": "lastName",
        "errorCode": "InvalidCharacters"
      }
    }
  ],
  "data": { "updateContact": null }
}
```

* `field` is the path of the input field inside `command`, for example `lastName`, `phones[0]`, `addresses[1].firstName` or `personalData.firstName`. `updateContact` and `updateOrganization` validate the saved member with the changes applied, so their errors can also name a saved field the request didn't send, such as a phone.
* `errorCode` is the rule: `InvalidCharacters` (name patterns), `HtmlTagsNotAllowed`, `UnsafeContent`, `InvalidEmail` (email format), or a FluentValidation code such as `NotEmptyValidator` or `MaximumLengthValidator`.
* `message` is in English. A client that shows the error to users should translate it by `errorCode`.

### User accounts

* `updatePersonalData` changes the email of the current user with `UserManager.SetEmailAsync`, as the Backoffice does: the new email is not confirmed, and the security stamp changes. A new security stamp ends the Backoffice sessions of the user at their next validation (`Authorization:SecurityStampValidationInterval`) and the sign-ins waiting for a second factor; access and refresh tokens already issued stay valid until they expire.
* `updateUser` changes only the phone number and the photo URL of a user, and replaces both with the values of the request: a field the request leaves out is cleared. A new phone number is set with `UserManager.SetPhoneNumberAsync`: it is not confirmed, and the security stamp changes. Every other field of `applicationUser` is ignored and marked as deprecated: the email is changed with `updatePersonalData`, the role of an organization member with `changeOrganizationContactRole`, and the password, the security stamp, the lockout, the user type, the store and the member are managed by the platform.

## References

* [Deployment](https://docs.virtocommerce.org/platform/developer-guide/Tutorials-and-How-tos/Tutorials/deploy-module-from-source-code/)
* [Installation](https://docs.virtocommerce.org/platform/user-guide/modules-installation/)
* [Home](https://virtocommerce.com)
* [Community](https://www.virtocommerce.org)
* [Download latest release](https://github.com/VirtoCommerce/vc-module-profile-experience-api/releases/latest)

## License
Copyright (c) Virto Solutions LTD.  All rights reserved.

This software is licensed under the Virto Commerce Open Software License (the "License"); you
may not use this file except in compliance with the License. You may
obtain a copy of the License at http://virtocommerce.com/opensourcelicense.

Unless required by the applicable law or agreed to in written form, the software
distributed under the License is provided on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
implied.
