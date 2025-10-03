# Permission Constants System

Sistem permission constants yang terorganisir untuk mengelola permissions di seluruh aplikasi dengan type-safe dan konsisten.

## Struktur

### 1. Base Classes
- **`PermissionConstants`**: Base class dengan common actions dan scopes
- **`IModulePermissions`**: Interface untuk module permission definitions
- **`PermissionRegistry`**: Central registry untuk semua permissions
- **`PermissionHelper`**: Helper methods untuk bekerja dengan permissions

### 2. Module Permission Classes
Setiap module memiliki permission constants sendiri:
- **`UserPermissions`**: User module permissions
- **`RolePermissions`**: Role module permissions  
- **`AuthenticationPermissions`**: Authentication module permissions

## Cara Penggunaan

### 1. Menggunakan Predefined Permissions

```csharp
// Menggunakan predefined permission objects
var userReadPermission = UserPermissions.ReadAll;
var roleAssignPermission = RolePermissions.AssignDepartment;

// Menggunakan constants untuk membuat custom permissions
var customPermission = Permission.Create(
    UserPermissions.RESOURCE, 
    UserPermissions.Actions.READ, 
    "team"
);
```

### 2. Di Endpoints

```csharp
// Menggunakan constants
.RequirePermission(UserPermissions.RESOURCE, UserPermissions.Actions.WRITE)

// Atau menggunakan extension methods yang sudah diupdate
.RequireUserWrite()
.RequireRoleAssign()
```

### 3. Menggunakan Permission Registry

```csharp
// Inject PermissionRegistry
public class SomeService
{
    private readonly PermissionRegistry _permissionRegistry;
    
    public SomeService(PermissionRegistry permissionRegistry)
    {
        _permissionRegistry = permissionRegistry;
    }
    
    public void ExampleUsage()
    {
        // Get all permissions
        var allPermissions = _permissionRegistry.GetAllPermissions();
        
        // Get permissions by module
        var userPermissions = _permissionRegistry.GetModulePermissions("Users");
        
        // Find specific permission
        var permission = _permissionRegistry.FindPermission("user", "read", "*");
        
        // Get statistics
        var stats = _permissionRegistry.GetStatistics();
    }
}
```

### 4. Membuat Role dengan Permissions

```csharp
// Admin role
var adminPermissions = new List<Permission>
{
    UserPermissions.ManageAll,
    RolePermissions.ManageAll,
    AuthenticationPermissions.AuthAdmin
};

// Manager role (department level)
var managerPermissions = new List<Permission>
{
    UserPermissions.ManageDepartment,
    RolePermissions.ManageDepartment,
    AuthenticationPermissions.ManageSessionDepartment
};

// Basic user role
var basicPermissions = new List<Permission>
{
    UserPermissions.ReadSelf,
    UserPermissions.UpdateSelf,
    AuthenticationPermissions.RefreshTokenSelf
};
```

## API Endpoints

Sistem ini juga menyediakan endpoints untuk discovery dan management:

- `GET /api/permissions` - Get all permissions
- `GET /api/permissions/modules` - Get permissions by module
- `GET /api/permissions/resources` - Get permissions by resource
- `GET /api/permissions/statistics` - Get permission statistics
- `GET /api/permissions/search` - Search permissions
- `GET /api/permissions/{resource}/{action}` - Find specific permission

## Keuntungan

1. **Type Safety**: Menggunakan constants mengurangi typo dan error
2. **Centralized**: Semua permissions terdaftar di satu tempat
3. **Discoverable**: API endpoints untuk melihat semua permissions
4. **Modular**: Setiap module mengelola permissions sendiri
5. **Consistent**: Menggunakan naming convention yang sama
6. **Extensible**: Mudah menambah permissions baru

## Menambah Permission Baru

### 1. Di Module Permission Class

```csharp
public static class UserPermissions
{
    // Tambah action baru
    public static class Actions
    {
        public const string EXPORT = "export";
    }
    
    // Tambah scope baru
    public static class Scopes
    {
        public const string TEAM = "team";
    }
    
    // Tambah predefined permission
    public static readonly Permission ExportTeam = Permission.Create(RESOURCE, Actions.EXPORT, Scopes.TEAM);
    
    // Update GetAllPermissions()
    public static IReadOnlyList<Permission> GetAllPermissions()
    {
        return new List<Permission>
        {
            // ... existing permissions
            ExportTeam
        };
    }
}
```

### 2. Update Extension Methods (Optional)

```csharp
public static TBuilder RequireUserExport<TBuilder>(this TBuilder builder) where TBuilder : IEndpointConventionBuilder
    => builder.RequirePermission(UserPermissions.RESOURCE, UserPermissions.Actions.EXPORT);
```

### 3. Gunakan di Endpoints

```csharp
.RequirePermission(UserPermissions.RESOURCE, UserPermissions.Actions.EXPORT, UserPermissions.Scopes.TEAM)
// atau
.RequireUserExport()
```

## Best Practices

1. **Gunakan Constants**: Selalu gunakan constants daripada hardcode strings
2. **Consistent Naming**: Ikuti naming convention yang ada
3. **Granular Permissions**: Buat permissions yang spesifik dan granular
4. **Document Scopes**: Jelaskan arti dari setiap scope
5. **Group Related**: Kelompokkan permissions yang related
6. **Test Coverage**: Test semua permission scenarios