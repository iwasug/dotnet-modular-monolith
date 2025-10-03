# Multi-Language Support Implementation

This document describes the comprehensive multi-language support implementation for the Enterprise User Management API.

## Overview

The API supports multiple languages through a custom JSON-based localization system, providing localized validation messages, error responses, and API documentation. This approach offers better maintainability and easier translation management compared to traditional .resx files.

## Supported Languages

- **English (en-US)** - Default language
- **Spanish (es-ES)** - Español
- **French (fr-FR)** - Français
- **German (de-DE)** - Deutsch
- **Portuguese (pt-BR)** - Português (Brasil)
- **Italian (it-IT)** - Italiano
- **Japanese (ja-JP)** - 日本語
- **Chinese (zh-CN)** - 中文 (简体)
- **Indonesian (id-ID)** - Bahasa Indonesia

## Culture Detection

The API determines the user's preferred language using the following priority order:

1. **Accept-Language Header** (Primary) - Standard HTTP header
2. **Query String Parameter** - `?culture=en-US`
3. **Cookie** - `UserCulture` cookie for persistence
4. **Fallback** - Default to `en-US` if none match

## Modular JSON Resource Files Structure

### Module-Specific Resources
```
src/Modules/
├── Users/Resources/
│   ├── user-messages.json      # Default (English)
│   ├── user-messages.id.json   # Indonesian
│   └── user-messages.es.json   # Spanish
├── Roles/Resources/
│   ├── role-messages.json      # Default (English)
│   ├── role-messages.id.json   # Indonesian
│   └── role-messages.es.json   # Spanish
└── Authentication/Resources/
    ├── auth-messages.json      # Default (English)
    ├── auth-messages.id.json   # Indonesian
    └── auth-messages.es.json   # Spanish
```

### API Documentation Resources
```
src/Api/Resources/
├── api-documentation.json     # Default (English)
├── api-documentation.es.json  # Spanish
├── api-documentation.fr.json  # French
├── api-documentation.de.json  # German
├── api-documentation.pt.json  # Portuguese
└── api-documentation.id.json  # Indonesian
```

### Shared Common Messages
```
src/Shared/Resources/
├── validation-messages.json    # Common validation messages
├── error-messages.json         # Common error messages
└── [language variants...]      # Language-specific versions
```

## Services

### ILocalizationService
Central service for retrieving localized strings across the application using JSON resource files.

```csharp
public interface ILocalizationService
{
    string GetString(string key, string? culture = null);
    string GetString(string key, params object[] args);
    string GetString(string key, string? culture, params object[] args);
    CultureInfo CurrentCulture { get; }
    CultureInfo CurrentUICulture { get; }
    string[] SupportedCultures { get; }
    bool IsCultureSupported(string culture);
}
```

### ModularJsonLocalizationService
Modular JSON-based implementation that loads resources from each module separately. Features:
- **Modular Structure**: Each module manages its own localization resources
- **Performance**: Pre-loads and caches all resources at startup
- **Fallback**: Automatic fallback to default language if translation missing
- **Isolation**: Module resources are isolated from each other
- **Maintainability**: Easier to manage resources per module
- **Scalability**: Easy to add new modules with their own resources

### Module-Specific Services
Each module has its own localization service:
- **IUserLocalizationService**: For Users module resources
- **IRoleLocalizationService**: For Roles module resources  
- **IAuthLocalizationService**: For Authentication module resources

### ILocalizedErrorService
Service for creating localized error responses.

```csharp
public interface ILocalizedErrorService
{
    Error CreateValidationError(string code, string messageKey, string? culture = null);
    Error CreateNotFoundError(string code, string messageKey, string? culture = null);
    Error CreateConflictError(string code, string messageKey, string? culture = null);
    // ... other error types
}
```

## Middleware

### LocalizedValidationMiddleware
Handles FluentValidation exceptions and returns localized error messages.

### GlobalExceptionMiddleware (Enhanced)
Enhanced to provide localized error messages for unhandled exceptions.

## Usage Examples

### Setting Language via Header
```http
GET /api/users
Accept-Language: es-ES
```

### Setting Language via Query Parameter
```http
GET /api/users?culture=fr-FR
```

### Setting Language via Cookie
```http
GET /api/users
Cookie: UserCulture=de-DE
```

### Setting Indonesian Language
```http
GET /api/users
Accept-Language: id-ID
```

## Modular Validator Localization

Validators now use module-specific localization services:

```csharp
public class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator(IUserLocalizationService userLocalizationService)
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage(userLocalizationService.GetString("EmailRequired"))
            .EmailAddress()
            .WithMessage(userLocalizationService.GetString("EmailInvalid"));
    }
}

public class CreateRoleValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleValidator(IRoleLocalizationService roleLocalizationService)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage(roleLocalizationService.GetString("RoleNameRequired"));
    }
}
```

## API Response Examples

### English Response
```json
{
  "error": {
    "code": "VALIDATION_FAILED",
    "message": "Validation failed",
    "type": "Validation"
  },
  "validationErrors": {
    "Email": ["Email is required"]
  }
}
```

### Spanish Response
```json
{
  "error": {
    "code": "VALIDATION_FAILED",
    "message": "Error de validación",
    "type": "Validation"
  },
  "validationErrors": {
    "Email": ["El correo electrónico es requerido"]
  }
}
```

### Indonesian Response
```json
{
  "error": {
    "code": "VALIDATION_FAILED",
    "message": "Validasi gagal",
    "type": "Validation"
  },
  "validationErrors": {
    "Email": ["Email wajib diisi"]
  }
}
```

## Testing Modular Localization

### Test Endpoints
- `GET /api/localization/cultures` - Get supported cultures
- `GET /api/localization/current-culture` - Get current request culture
- `GET /api/localization/test-message/{key}` - Test message retrieval
- `POST /api/localization/test-validation` - Test validation messages

### Modular Test Endpoints
- `GET /api/modular-localization-test/module/{moduleName}/messages` - Test module-specific messages
- `GET /api/modular-localization-test/all-modules` - Test all module localizations
- `GET /api/modular-localization-test/module-structure` - Get modular structure info
- `GET /api/modular-localization-test/module/{moduleName}/culture/{culture}` - Test module with specific culture

### Example Test Request
```http
GET /api/localization/test-message/EmailRequired
Accept-Language: es-ES
```

Response:
```json
{
  "key": "EmailRequired",
  "message": "El correo electrónico es requerido",
  "culture": "es-ES"
}
```

### Indonesian Test Request
```http
GET /api/localization/test-message/EmailRequired
Accept-Language: id-ID
```

Response:
```json
{
  "key": "EmailRequired",
  "message": "Email wajib diisi",
  "culture": "id-ID"
}
```

## Configuration

### Program.cs Setup
```csharp
// Add localization services
builder.Services.AddComprehensiveLocalization();

// Use localization middleware
app.UseComprehensiveLocalization();
app.UseLocalizedValidation();
```

### Supported Culture Configuration
Cultures are configured in `LocalizationExtensions.cs`:

```csharp
var supportedCultures = new[]
{
    new CultureInfo("en-US"), // Default
    new CultureInfo("es-ES"), // Spanish
    new CultureInfo("fr-FR"), // French
    new CultureInfo("de-DE"), // German
    // ... additional cultures
};
```

## Adding New Languages

1. **Create JSON Resource Files**: Add `.json` files for the new culture
2. **Update Supported Cultures**: Add the culture to the configuration
3. **Test**: Use the test endpoints to verify translations

### Example: Adding Italian (it-IT)
1. Create `api-documentation.it.json`
2. Create `validation-messages.it.json`
3. Create `error-messages.it.json`
4. Add `"it-IT"` to supported cultures array in `JsonLocalizationService`

### JSON File Format
```json
{
  "EmailRequired": "Email is required",
  "EmailInvalid": "Email must be a valid email address",
  "PasswordRequired": "Password is required"
}
```

## Best Practices

1. **Consistent Keys**: Use consistent resource keys across all languages
2. **Fallback Handling**: Always provide fallback to default language
3. **Context-Aware**: Consider cultural context, not just translation
4. **Testing**: Test all supported languages regularly
5. **Performance**: JSON resources are pre-loaded and cached for optimal performance
6. **JSON Structure**: Keep JSON files well-formatted and organized
7. **Version Control**: JSON files are easier to track changes in version control
8. **Translation Management**: JSON format makes it easier for translators to work with

## Performance Considerations

- JSON resources are pre-loaded and cached at application startup
- All translations are kept in memory for fast access
- Cultures are cached per request
- Fallback mechanism prevents missing translations
- Minimal overhead for culture detection
- JSON parsing happens only once during application startup

## Troubleshooting

### Common Issues

1. **Missing Translations**: Check JSON file naming and ensure files are in correct directories
2. **Culture Not Detected**: Verify Accept-Language header format
3. **Fallback Not Working**: Ensure default JSON files exist
4. **JSON Parse Errors**: Validate JSON syntax using a JSON validator
5. **File Not Found**: Check file paths and ensure JSON files are included in build

### Debug Endpoints

Use the localization test endpoints to debug issues:
- Check current culture detection
- Verify message retrieval
- Test validation message localization