using System.Text.Json;
using GiantGroupMobileApp.Models;

namespace GiantGroupMobileApp.Services;

public interface ISessionService
{
    UserSession? Current { get; }
    string? Token { get; }
    bool IsSignedIn { get; }
    /// <summary>Raised (possibly on a background thread) when the server rejects the token.</summary>
    event Action? SessionExpired;
    Task<UserSession?> RestoreAsync();
    Task SignInAsync(UserSession session);
    Task SignOutAsync();
    void NotifyExpired();
}

/// <summary>Keeps the bearer token in the platform keystore (Keychain / Android Keystore) via SecureStorage.</summary>
public class SessionService : ISessionService
{
    private const string Key = "gg.session";
    private UserSession? _current;

    public UserSession? Current => _current;
    public string? Token => _current?.Token;
    public bool IsSignedIn => _current != null && !_current.IsExpired;
    public event Action? SessionExpired;

    public async Task<UserSession?> RestoreAsync()
    {
        try
        {
            var json = await SecureStorage.Default.GetAsync(Key);
            if (string.IsNullOrEmpty(json)) return null;
            var s = JsonSerializer.Deserialize<UserSession>(json);
            if (s == null || s.IsExpired) { await SignOutAsync(); return null; }
            _current = s;
            return s;
        }
        catch (Exception)
        {
            // Keystore can be unavailable/corrupt after restores; treat as signed out.
            SecureStorage.Default.Remove(Key);
            return null;
        }
    }

    public async Task SignInAsync(UserSession session)
    {
        _current = session;
        try { await SecureStorage.Default.SetAsync(Key, JsonSerializer.Serialize(session)); }
        catch (Exception) { /* still signed in for this run */ }
    }

    public Task SignOutAsync()
    {
        _current = null;
        try { SecureStorage.Default.Remove(Key); } catch (Exception) { }
        return Task.CompletedTask;
    }

    public void NotifyExpired()
    {
        if (_current == null) return;
        _current = null;
        try { SecureStorage.Default.Remove(Key); } catch (Exception) { }
        SessionExpired?.Invoke();
    }
}
