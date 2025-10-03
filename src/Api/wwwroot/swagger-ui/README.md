# Enterprise User Management API Documentation

## Overview

This directory contains custom assets for the Swagger UI documentation interface of the Enterprise User Management API.

## Files

### custom.css
Custom CSS styles that enhance the Swagger UI appearance with:
- Modern color scheme and typography
- Responsive design for mobile devices
- Dark mode support
- Enhanced visual hierarchy
- Custom animations and transitions
- Print-friendly styles

### custom.js
Custom JavaScript that adds enhanced functionality:
- **Language Selector**: Switch between supported languages (English, Spanish, French, German)
- **Authorization Enhancement**: Auto-save and restore JWT tokens
- **Keyboard Shortcuts**: 
  - `Ctrl/Cmd + K`: Focus search/filter
  - `Ctrl/Cmd + Enter`: Execute current operation
  - `Escape`: Close modals
- **Copy to Clipboard**: Copy code examples with one click
- **Request Timing**: Display request duration and status
- **Correlation ID**: Automatic request correlation for tracing

## Features

### Multi-Language Support
The API documentation supports localization in multiple languages:
- **English (en-US)**: Default language
- **Spanish (es-ES)**: Full translation of API descriptions and error messages
- **French (fr-FR)**: Complete French localization
- **German (de-DE)**: German translation support

### JWT Authentication
The Swagger UI is configured with JWT Bearer token authentication:
1. Click the "Authorize" button in the top-right corner
2. Enter your JWT token in the format: `Bearer <your-token>`
3. The token will be automatically saved and restored in future sessions

### Enhanced Documentation
- **Comprehensive Examples**: Real-world request/response examples for all endpoints
- **Detailed Descriptions**: Clear explanations of all parameters and responses
- **Error Handling**: Complete documentation of error responses and status codes
- **Rate Limiting**: Information about API rate limits and headers

### Developer Experience
- **Auto-completion**: Enhanced input validation and suggestions
- **Request Correlation**: Automatic correlation IDs for request tracing
- **Performance Metrics**: Request timing and performance information
- **Responsive Design**: Optimized for desktop, tablet, and mobile devices

## Usage

### Accessing the Documentation
- **Development**: Navigate to `/api-docs` when running the application locally
- **Production**: The documentation is available at the same path with security enhancements

### Authentication Flow
1. Use the `/api/auth/login` endpoint to obtain a JWT token
2. Copy the `accessToken` from the response
3. Click "Authorize" in Swagger UI and paste the token
4. All subsequent requests will include the authorization header

### Language Selection
1. Look for the language selector in the API information section
2. Choose your preferred language from the dropdown
3. The interface will update to show localized content
4. Your preference is saved for future sessions

## Customization

### Adding New Languages
To add support for additional languages:
1. Create new resource files in `src/Api/Resources/`
2. Update the `ApiDocumentationService` to include the new language
3. Add the language code to the supported cultures array

### Modifying Styles
The `custom.css` file uses CSS custom properties (variables) for easy theming:
```css
:root {
    --primary-color: #2563eb;
    --secondary-color: #1e40af;
    --success-color: #059669;
    /* ... other variables */
}
```

### Extending Functionality
The `custom.js` file is modular and can be extended with additional features:
- Add new keyboard shortcuts
- Implement custom request/response transformations
- Add integration with external monitoring tools

## Security Considerations

### Production Deployment
- Validator is disabled in production for security
- CORS is properly configured for allowed origins
- Rate limiting is enforced on all endpoints
- Security headers are automatically added

### Token Management
- JWT tokens are stored securely in localStorage
- Tokens are automatically cleared on logout
- Refresh token rotation is supported

## Browser Support

The custom assets are compatible with:
- Chrome 90+
- Firefox 88+
- Safari 14+
- Edge 90+

## Performance

### Optimizations
- CSS is minified and optimized for production
- JavaScript uses modern ES6+ features for better performance
- Lazy loading for non-critical features
- Efficient DOM manipulation and event handling

### Monitoring
- Request timing is automatically tracked
- Performance metrics are displayed in the UI
- Correlation IDs enable distributed tracing

## Troubleshooting

### Common Issues
1. **Authorization not working**: Ensure the token format is `Bearer <token>`
2. **Language not changing**: Check browser console for JavaScript errors
3. **Styles not loading**: Verify the CSS file path and server configuration

### Debug Mode
Enable debug mode by adding `?debug=true` to the documentation URL for additional logging and diagnostic information.