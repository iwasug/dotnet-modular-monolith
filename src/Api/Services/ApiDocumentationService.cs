using ModularMonolith.Shared.Services;

namespace ModularMonolith.Api.Services;

/// <summary>
/// Service for providing localized API documentation content
/// </summary>
public sealed class ApiDocumentationService(ILocalizationService localizationService) : IApiDocumentationService
{
    private readonly string[] _supportedCultures = { "en-US", "es-ES", "fr-FR", "de-DE", "pt-BR", "it-IT", "ja-JP", "zh-CN", "id-ID" };

    public string GetApiTitle(string? culture = null)
    {
        return localizationService.GetString("ApiTitle", culture);
    }

    public string GetApiDescription(string? culture = null)
    {
        var description = localizationService.GetString("ApiDescription", culture);

        // Add additional features description based on culture
        var featuresDescription = culture?.StartsWith("es") == true
            ? GetSpanishFeaturesDescription()
            : culture?.StartsWith("fr") == true
            ? GetFrenchFeaturesDescription()
            : culture?.StartsWith("de") == true
            ? GetGermanFeaturesDescription()
            : culture?.StartsWith("pt") == true
            ? GetPortugueseFeaturesDescription()
            : culture?.StartsWith("id") == true
            ? GetIndonesianFeaturesDescription()
            : GetEnglishFeaturesDescription();

        return $"{description}\n\n{featuresDescription}";
    }

    public string GetAuthenticationDescription(string? culture = null)
    {
        var baseDescription = localizationService.GetString("AuthenticationDescription", culture);

        var additionalInfo = culture?.StartsWith("es") == true
            ? "\n\n**Cómo obtener un token**:\n1. Llama al endpoint `/api/auth/login` con credenciales válidas\n2. Usa el `accessToken` devuelto en el header Authorization\n3. Actualiza el token usando `/api/auth/refresh` antes de que expire"
            : culture?.StartsWith("fr") == true
            ? "\n\n**Comment obtenir un token**:\n1. Appelez l'endpoint `/api/auth/login` avec des identifiants valides\n2. Utilisez l'`accessToken` retourné dans l'en-tête Authorization\n3. Actualisez le token en utilisant `/api/auth/refresh` avant qu'il n'expire"
            : culture?.StartsWith("de") == true
            ? "\n\n**Wie man einen Token erhält**:\n1. Rufen Sie den `/api/auth/login` Endpoint mit gültigen Anmeldedaten auf\n2. Verwenden Sie das zurückgegebene `accessToken` im Authorization-Header\n3. Aktualisieren Sie den Token mit `/api/auth/refresh` bevor er abläuft"
            : culture?.StartsWith("pt") == true
            ? "\n\n**Como obter um token**:\n1. Chame o endpoint `/api/auth/login` com credenciais válidas\n2. Use o `accessToken` retornado no header Authorization\n3. Atualize o token usando `/api/auth/refresh` antes que expire"
            : culture?.StartsWith("id") == true
            ? "\n\n**Cara mendapatkan token**:\n1. Panggil endpoint `/api/auth/login` dengan kredensial yang valid\n2. Gunakan `accessToken` yang dikembalikan di header Authorization\n3. Perbarui token menggunakan `/api/auth/refresh` sebelum kedaluwarsa"
            : "\n\n**How to obtain a token**:\n1. Call the `/api/auth/login` endpoint with valid credentials\n2. Use the returned `accessToken` in the Authorization header\n3. Refresh the token using `/api/auth/refresh` before it expires";

        return baseDescription + additionalInfo;
    }

    public string GetErrorMessage(string errorType, string? culture = null)
    {
        return localizationService.GetString($"{errorType}Error", culture);
    }

    public string[] GetSupportedCultures()
    {
        return _supportedCultures;
    }

    private static string GetPortugueseFeaturesDescription()
    {
        return """
            ## Recursos
            - **Gerenciamento de Usuários**: Operações CRUD completas para contas de usuário
            - **Controle de Acesso Baseado em Funções**: Sistema de permissões granular com modelo recurso-ação-escopo
            - **Autenticação JWT**: Autenticação segura baseada em tokens com rotação de tokens de atualização
            - **Suporte Multi-idioma**: Respostas localizadas baseadas no cabeçalho Accept-Language
            - **Cache**: Cache Redis e em memória para desempenho otimizado
            - **Monitoramento**: Verificações de saúde abrangentes e registro estruturado
            
            ## Autenticação
            Esta API usa tokens JWT Bearer para autenticação. Para acessar endpoints protegidos:
            1. Obtenha um token chamando o endpoint `/api/auth/login`
            2. Inclua o token no cabeçalho Authorization: `Bearer <seu-token>`
            3. Atualize os tokens usando o endpoint `/api/auth/refresh` quando necessário
            
            ## Permissões
            A API implementa um sistema de permissões granular com o formato: `recurso:ação:escopo`
            - **Recursos**: user, role, auth
            - **Ações**: read, write, delete, assign
            - **Escopos**: own, team, organization, global
            """;
    }

    private static string GetIndonesianFeaturesDescription()
    {
        return """
            ## Fitur
            - **Manajemen Pengguna**: Operasi CRUD lengkap untuk akun pengguna
            - **Kontrol Akses Berbasis Peran**: Sistem izin granular dengan model sumber daya-aksi-cakupan
            - **Autentikasi JWT**: Autentikasi aman berbasis token dengan rotasi token penyegaran
            - **Dukungan Multi-bahasa**: Respons terlokalisasi berdasarkan header Accept-Language
            - **Cache**: Cache Redis dan dalam memori untuk performa optimal
            - **Pemantauan**: Pemeriksaan kesehatan komprehensif dan pencatatan terstruktur
            
            ## Autentikasi
            API ini menggunakan token JWT Bearer untuk autentikasi. Untuk mengakses endpoint yang dilindungi:
            1. Dapatkan token dengan memanggil endpoint `/api/auth/login`
            2. Sertakan token di header Authorization: `Bearer <token-anda>`
            3. Perbarui token menggunakan endpoint `/api/auth/refresh` saat diperlukan
            
            ## Izin
            API mengimplementasikan sistem izin granular dengan format: `sumber-daya:aksi:cakupan`
            - **Sumber Daya**: user, role, auth
            - **Aksi**: read, write, delete, assign
            - **Cakupan**: own, team, organization, global
            """;
    }

    private static string GetEnglishFeaturesDescription()
    {
        return """
            ## Features
            - **User Management**: Complete CRUD operations for user accounts
            - **Role-Based Access Control**: Granular permission system with resource-action-scope model
            - **JWT Authentication**: Secure token-based authentication with refresh token rotation
            - **Multi-Language Support**: Localized responses based on Accept-Language header
            - **Caching**: Redis and In-Memory caching for optimal performance
            - **Monitoring**: Comprehensive health checks and structured logging
            
            ## Authentication
            This API uses JWT Bearer tokens for authentication. To access protected endpoints:
            1. Obtain a token by calling the `/api/auth/login` endpoint
            2. Include the token in the Authorization header: `Bearer <your-token>`
            3. Refresh tokens using the `/api/auth/refresh` endpoint when needed
            
            ## Permissions
            The API implements a granular permission system with the format: `resource:action:scope`
            - **Resources**: user, role, auth
            - **Actions**: read, write, delete, assign
            - **Scopes**: own, team, organization, global
            """;
    }

    private static string GetSpanishFeaturesDescription()
    {
        return """
            ## Características
            - **Gestión de Usuarios**: Operaciones CRUD completas para cuentas de usuario
            - **Control de Acceso Basado en Roles**: Sistema de permisos granular con modelo recurso-acción-alcance
            - **Autenticación JWT**: Autenticación segura basada en tokens con rotación de tokens de actualización
            - **Soporte Multi-idioma**: Respuestas localizadas basadas en el encabezado Accept-Language
            - **Caché**: Caché Redis y en memoria para rendimiento óptimo
            - **Monitoreo**: Verificaciones de salud integrales y registro estructurado
            
            ## Autenticación
            Esta API utiliza tokens JWT Bearer para autenticación. Para acceder a endpoints protegidos:
            1. Obtén un token llamando al endpoint `/api/auth/login`
            2. Incluye el token en el encabezado Authorization: `Bearer <tu-token>`
            3. Actualiza los tokens usando el endpoint `/api/auth/refresh` cuando sea necesario
            
            ## Permisos
            La API implementa un sistema de permisos granular con el formato: `recurso:acción:alcance`
            - **Recursos**: user, role, auth
            - **Acciones**: read, write, delete, assign
            - **Alcances**: own, team, organization, global
            """;
    }

    private static string GetFrenchFeaturesDescription()
    {
        return """
            ## Fonctionnalités
            - **Gestion des Utilisateurs**: Opérations CRUD complètes pour les comptes utilisateur
            - **Contrôle d'Accès Basé sur les Rôles**: Système de permissions granulaire avec modèle ressource-action-portée
            - **Authentification JWT**: Authentification sécurisée basée sur des tokens avec rotation des tokens de rafraîchissement
            - **Support Multi-langues**: Réponses localisées basées sur l'en-tête Accept-Language
            - **Cache**: Cache Redis et en mémoire pour des performances optimales
            - **Surveillance**: Vérifications de santé complètes et journalisation structurée
            
            ## Authentification
            Cette API utilise des tokens JWT Bearer pour l'authentification. Pour accéder aux endpoints protégés:
            1. Obtenez un token en appelant l'endpoint `/api/auth/login`
            2. Incluez le token dans l'en-tête Authorization: `Bearer <votre-token>`
            3. Actualisez les tokens en utilisant l'endpoint `/api/auth/refresh` si nécessaire
            
            ## Permissions
            L'API implémente un système de permissions granulaire avec le format: `ressource:action:portée`
            - **Ressources**: user, role, auth
            - **Actions**: read, write, delete, assign
            - **Portées**: own, team, organization, global
            """;
    }

    private static string GetGermanFeaturesDescription()
    {
        return """
            ## Funktionen
            - **Benutzerverwaltung**: Vollständige CRUD-Operationen für Benutzerkonten
            - **Rollenbasierte Zugriffskontrolle**: Granulares Berechtigungssystem mit Ressource-Aktion-Bereich-Modell
            - **JWT-Authentifizierung**: Sichere token-basierte Authentifizierung mit Refresh-Token-Rotation
            - **Mehrsprachige Unterstützung**: Lokalisierte Antworten basierend auf Accept-Language-Header
            - **Caching**: Redis und In-Memory-Caching für optimale Leistung
            - **Überwachung**: Umfassende Gesundheitsprüfungen und strukturierte Protokollierung
            
            ## Authentifizierung
            Diese API verwendet JWT Bearer-Token für die Authentifizierung. Um auf geschützte Endpoints zuzugreifen:
            1. Erhalten Sie einen Token durch Aufruf des `/api/auth/login` Endpoints
            2. Fügen Sie den Token in den Authorization-Header ein: `Bearer <ihr-token>`
            3. Aktualisieren Sie Token mit dem `/api/auth/refresh` Endpoint bei Bedarf
            
            ## Berechtigungen
            Die API implementiert ein granulares Berechtigungssystem mit dem Format: `ressource:aktion:bereich`
            - **Ressourcen**: user, role, auth
            - **Aktionen**: read, write, delete, assign
            - **Bereiche**: own, team, organization, global
            """;
    }
}