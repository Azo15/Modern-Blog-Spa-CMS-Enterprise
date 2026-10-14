using BusinessLayer.Security;
using DataAccesLayer.Concrete;
using EntityLayer.Concrete;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WebUI.Data;

namespace WebUI.Infrastructure;

/// <summary>Deployment-only schema preparation; normal web startup never gains DDL credentials.</summary>
public static class DeploymentDatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var bootstrapConnection = configuration.GetConnectionString("BootstrapConnection");
        if (!string.IsNullOrWhiteSpace(bootstrapConnection))
            await ProvisionDatabaseAsync(bootstrapConnection, configuration, cancellationToken);

        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync(cancellationToken);

        // Hangfire schema preparation uses the short-lived migrator db_owner login.
        // The normal web host sets PrepareSchemaIfNecessary=false.
        var storage = new SqlServerStorage(
            configuration.GetConnectionString("Hangfire") ?? configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Migration database connection is required."),
            new SqlServerStorageOptions { PrepareSchemaIfNecessary = true });

        if (!string.IsNullOrWhiteSpace(bootstrapConnection))
            await context.Database.ExecuteSqlRawAsync(
                "GRANT EXECUTE ON SCHEMA::[HangFire] TO [modern_blog_app];", cancellationToken);

        await DataSeeder.SeedRolesAsync(services);
        await BootstrapAdminAsync(scope.ServiceProvider, configuration);
    }

    private static async Task ProvisionDatabaseAsync(string connectionString, IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var migratorPassword = RequiredSqlPassword(configuration["DatabaseBootstrap:MigratorPassword"]);
        var applicationPassword = RequiredSqlPassword(configuration["DatabaseBootstrap:ApplicationPassword"]);
        var connectionBuilder = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" };
        await using var connection = new SqlConnection(connectionBuilder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandTimeout = 120;
        command.CommandText = """
            DECLARE @lockResult int;
            EXEC @lockResult = sys.sp_getapplock @Resource = N'ModernBlog_DeploymentBootstrap',
                @LockMode = N'Exclusive', @LockOwner = N'Session', @LockTimeout = 60000;
            IF @lockResult < 0 THROW 51000, 'Deployment bootstrap lock unavailable.', 1;

            IF DB_ID(N'xspa_db') IS NULL CREATE DATABASE [xspa_db];

            DECLARE @sql nvarchar(max);
            SET @sql = CASE WHEN SUSER_ID(N'modern_blog_migrator') IS NULL
                THEN N'CREATE LOGIN [modern_blog_migrator] WITH PASSWORD = '
                ELSE N'ALTER LOGIN [modern_blog_migrator] WITH PASSWORD = ' END
                + QUOTENAME(@MigratorPassword, '''') + N', CHECK_POLICY = ON;';
            EXEC sys.sp_executesql @sql;
            SET @sql = CASE WHEN SUSER_ID(N'modern_blog_app') IS NULL
                THEN N'CREATE LOGIN [modern_blog_app] WITH PASSWORD = '
                ELSE N'ALTER LOGIN [modern_blog_app] WITH PASSWORD = ' END
                + QUOTENAME(@ApplicationPassword, '''') + N', CHECK_POLICY = ON;';
            EXEC sys.sp_executesql @sql;

            """;
        command.Parameters.AddWithValue("@MigratorPassword", migratorPassword);
        command.Parameters.AddWithValue("@ApplicationPassword", applicationPassword);
        await command.ExecuteNonQueryAsync(cancellationToken);

        // Compile this batch only after CREATE DATABASE has completed.
        command.Parameters.Clear();
        command.CommandText = """
            USE [xspa_db];
            IF DATABASE_PRINCIPAL_ID(N'modern_blog_migrator') IS NULL
                CREATE USER [modern_blog_migrator] FOR LOGIN [modern_blog_migrator];
            IF DATABASE_PRINCIPAL_ID(N'modern_blog_app') IS NULL
                CREATE USER [modern_blog_app] FOR LOGIN [modern_blog_app];
            ALTER ROLE [db_owner] ADD MEMBER [modern_blog_migrator];
            IF IS_ROLEMEMBER(N'db_owner', N'modern_blog_app') = 1
                ALTER ROLE [db_owner] DROP MEMBER [modern_blog_app];
            IF IS_ROLEMEMBER(N'db_ddladmin', N'modern_blog_app') = 1
                ALTER ROLE [db_ddladmin] DROP MEMBER [modern_blog_app];
            ALTER ROLE [db_datareader] ADD MEMBER [modern_blog_app];
            ALTER ROLE [db_datawriter] ADD MEMBER [modern_blog_app];
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
        // Closing this connection releases the session-owned bootstrap lock.
    }

    private static string RequiredSqlPassword(string? password)
    {
        // Compose provides connection strings; avoid characters requiring further escaping.
        // Passwords still use SQL parameters and QUOTENAME inside the login DDL.
        if (string.IsNullOrWhiteSpace(password) || password.Length is < 16 or > 128 ||
            password.IndexOfAny(['"', ';', '\r', '\n']) >= 0 ||
            !password.Any(char.IsUpper) || !password.Any(char.IsLower) ||
            !password.Any(char.IsDigit) || !password.Any(c => !char.IsLetterOrDigit(c)))
            throw new InvalidOperationException("Database bootstrap passwords must contain 16-128 characters, upper/lowercase, a digit and symbol; double quotes, semicolons and newlines are unsupported.");
        return password;
    }

    private static async Task BootstrapAdminAsync(IServiceProvider services, IConfiguration configuration)
    {
        var email = configuration["BootstrapAdmin:Email"];
        var password = configuration["BootstrapAdmin:Password"];
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(password)) return;
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Bootstrap admin email and password must be provided together.");

        var manager = services.GetRequiredService<UserManager<AppUser>>();
        if ((await manager.GetUsersInRoleAsync(AppRoles.Admin)).Count > 0) return;
        if (password.Length < 14 || !password.Any(char.IsUpper) || !password.Any(char.IsLower) ||
            !password.Any(char.IsDigit) || !password.Any(c => !char.IsLetterOrDigit(c)))
            throw new InvalidOperationException("Bootstrap admin password must have at least 14 characters with upper/lowercase, digit and symbol.");
        if (await manager.FindByEmailAsync(email) is not null)
            throw new InvalidOperationException("Bootstrap admin must use an unused account email; existing accounts are never promoted automatically.");

        await using var transaction = await services.GetRequiredService<AppDbContext>().Database.BeginTransactionAsync();
        var admin = new AppUser { UserName = email, Email = email, EmailConfirmed = true,
            FirstName = "System", LastName = "Administrator", LockoutEnabled = true };
        Ensure(await manager.CreateAsync(admin, password));
        Ensure(await manager.AddToRoleAsync(admin, AppRoles.Admin));
        await transaction.CommitAsync();
    }

    private static void Ensure(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException("Bootstrap admin creation failed; verify Identity password/account requirements.");
    }
}
