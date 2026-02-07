using System.Text.Json;
using UserWindowsService.Users;

namespace UserWindowsService.Persistence;

public class FileUserRepository : IUserRepository
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    private List<User> _users = new();

    public FileUserRepository(IHostEnvironment env)
    {
        var dataDir = Path.Combine(env.ContentRootPath, "Data");
        Directory.CreateDirectory(dataDir);

        _filePath = Path.Combine(dataDir, "users.json");
    }

    public async Task InitializeAsync(CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (!File.Exists(_filePath))
            {
                _users = new List<User>();
                return;
            }

            var json = await File.ReadAllTextAsync(_filePath, ct);
            _users = string.IsNullOrWhiteSpace(json)
                ? new List<User>()
                : (JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>());
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task SaveAsync(CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(_users, _jsonOptions);
        await File.WriteAllTextAsync(_filePath, json, ct);
    }

    public async Task<List<User>> GetAllAsync(CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try { return _users.ToList(); }
        finally { _lock.Release(); }
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try { return _users.FirstOrDefault(u => u.Id == id); }
        finally { _lock.Release(); }
    }

    public async Task AddAsync(User user, CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (_users.Any(u => u.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Email already exists.");

            _users.Add(user);
            await SaveAsync(ct);
        }
        finally { _lock.Release(); }
    }

    public async Task UpdateAsync(User user, CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var idx = _users.FindIndex(u => u.Id == user.Id);
            if (idx < 0) throw new KeyNotFoundException("User not found.");

            _users[idx] = user;
            await SaveAsync(ct);
        }
        finally { _lock.Release(); }
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            _users.RemoveAll(u => u.Id == id);
            await SaveAsync(ct);
        }
        finally { _lock.Release(); }
    }
}
