using CecoChat.Data.User;
using CecoChat.Npgsql;
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
        bool initialized = _initializer.Initialize(_options.Init, database, typeof(UserDbContext).Assembly);

        if (initialized && _options.Seed)
        {
            if (_options.SeedConsoleClientUsers)
            {
                await SeedConsoleClientUsers(cancellationToken);
                _logger.LogInformation("Seeded database with console client users.");
            }
            if (_options.SeedLoadTestingUsers)
            {
                await SeedLoadTestingUsers(_options.SeedLoadTestingUserCount, cancellationToken);
                _logger.LogInformation("Seeded database with {0} load testing users.", _options.SeedLoadTestingUserCount);
            }
        }

        _userDbInitHealthCheck.IsReady = true;
    }

    private async Task SeedConsoleClientUsers(CancellationToken ct)
    {
        await _dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE public.\"Profiles\"", ct);

        ProfileEntity[] profiles =
        {
            new()
            {
                UserId = 1,
                UserName = "bob1",
                DisplayName = "Robert",
                AvatarUrl = "https://cdn.cecochat.com/avatars/bob1.jpg",
                Email = "bob1@cecochat.com",
                Phone = "+359888111111"
            },
            new()
            {
                UserId = 2,
                UserName = "alice2",
                DisplayName = "Alice in Wonderland",
                AvatarUrl = "https://cdn.cecochat.com/avatars/alice2.jpg",
                Email = "alice2@cecochat.com",
                Phone = "+359888222222"
            },
            new()
            {
                UserId = 3,
                UserName = "john3",
                DisplayName = "Sir John",
                AvatarUrl = "https://cdn.cecochat.com/avatars/john3.jpg",
                Email = "john3@cecochat.com",
                Phone = "+359888333333"
            },
            new()
            {
                UserId = 1200,
                UserName = "peter1200",
                DisplayName = "Peter the Great",
                AvatarUrl = "https://cdn.cecochat.com/avatars/peter1200.jpg",
                Email = "peter1200@cecochat.com",
                Phone = "+359888120012"
            }
        };

        _dbContext.Profiles.AddRange(profiles);
        await _dbContext.SaveChangesAsync(ct);
        _dbContext.ChangeTracker.Clear();
    }

    private async Task SeedLoadTestingUsers(int userCount, CancellationToken ct)
    {
        await _dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE public.\"Profiles\"", ct);

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

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
