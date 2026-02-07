using UserWindowsService.Persistence;

namespace UserWindowsService.Startup;

public class RepositoryInitializer : IHostedService
{
    private readonly FileUserRepository _repo;

    public RepositoryInitializer(FileUserRepository repo) => _repo = repo;

    public async Task StartAsync(CancellationToken cancellationToken)
        => await _repo.InitializeAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
