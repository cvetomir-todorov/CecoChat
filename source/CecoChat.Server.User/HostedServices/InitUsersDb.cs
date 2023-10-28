using CecoChat.Data.User;
using CecoChat.Npgsql;
using CecoChat.Server.User.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace CecoChat.Server.User.HostedServices;

public sealed class InitUsersDb : IHostedService
{
    private readonly ILogger _logger;
    private readonly UserDbOptions _options;
    private readonly INpgsqlDbInitializer _initializer;
    private readonly UserDbContext _dbContext;
    private readonly UserDbInitHealthCheck _userDbInitHealthCheck;

    public InitUsersDb(
        ILogger<InitUsersDb> logger,
        IOptions<UserDbOptions> options,
        INpgsqlDbInitializer initializer,
        UserDbContext dbContext,
        UserDbInitHealthCheck userDbInitHealthCheck)
    {
        _logger = logger;
        _options = options.Value;
        _initializer = initializer;
        _dbContext = dbContext;
        _userDbInitHealthCheck = userDbInitHealthCheck;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        string database = new NpgsqlConnectionStringBuilder(_options.Connect.ConnectionString).Database!;
        _initializer.Initialize(_options.Init, database, typeof(UserDbContext).Assembly);

        if (_options.Seed)
        {
            _logger.LogInformation("Seeding the database...");

            int connectionCount = await DeleteAllConnections(cancellationToken);
            int profileCount = await DeleteAllProfiles(cancellationToken);
            _logger.LogInformation("Deleted {ProfileCount} profiles and {ConnectionCount} connections", profileCount, connectionCount);

            if (_options.SeedConsoleClientUsers)
            {
                await SeedConsoleClientUsers(cancellationToken);
                _logger.LogInformation("Seeded database with console client users.");
            }
            if (_options.SeedLoadTestingUsers)
            {
                await SeedLoadTestingUsers(_options.SeedLoadTestingUserCount, cancellationToken);
                _logger.LogInformation("Seeded database with {UserCount} load testing users.", _options.SeedLoadTestingUserCount);
            }
        }

        _userDbInitHealthCheck.IsReady = true;
    }

    private async Task SeedConsoleClientUsers(CancellationToken ct)
    {
        ProfileEntity[] profiles =
        {
            new()
            {
                UserId = 1,
                UserName = "bobby",
                DisplayName = "Robert",
                Phone = "+359888111111"
            },
            new()
            {
                UserId = 2,
                UserName = "alice",
                DisplayName = "Alice in Wonderland",
                Phone = "+359888222222"
            },
            new()
            {
                UserId = 3,
                UserName = "john",
                DisplayName = "Sir John",
                Phone = "+359888333333"
            },
            new()
            {
                UserId = 1200,
                UserName = "peter",
                DisplayName = "Peter the Great",
                Phone = "+359888120012"
            }
        };

        PasswordHasher passwordHasher = new();

        foreach (ProfileEntity profile in profiles)
        {
            profile.Version = DateTime.UtcNow;
            profile.Password = passwordHasher.Hash("secret12");
            profile.AvatarUrl = $"https://cdn.cecochat.com/avatars/{profile.UserName}.jpg";
            profile.Email = $"{profile.UserName}@cecochat.com";
        }

        _dbContext.Profiles.AddRange(profiles);
        await _dbContext.SaveChangesAsync(ct);
        _dbContext.ChangeTracker.Clear();
    }

    private async Task SeedLoadTestingUsers(int userCount, CancellationToken ct)
    {
        ProfileEntity[] profiles = new ProfileEntity[userCount];
        for (int i = 0; i < userCount; ++i)
        {
            long userId = i + 1;
            profiles[i] = new()
            {
                UserId = userId,
                UserName = $"user{userId}",
                DisplayName = $"User {userId}",
                AvatarUrl = $"https://cdn.cecochat.com/avatars/user{userId}.jpg",
                Email = $"user{userId}@cecochat.com",
                Phone = "+359888000000"
            };
        }

        _dbContext.Profiles.AddRange(profiles);
        await _dbContext.SaveChangesAsync(ct);
        _dbContext.ChangeTracker.Clear();
    }

    private Task<int> DeleteAllConnections(CancellationToken ct)
    {
        return _dbContext.Database.ExecuteSqlRawAsync("DELETE from public.\"Connections\"", ct);
    }

    private Task<int> DeleteAllProfiles(CancellationToken ct)
    {
        return _dbContext.Database.ExecuteSqlRawAsync("DELETE from public.\"Profiles\"", ct);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
