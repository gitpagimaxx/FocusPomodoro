using System.Text.Json;
using System.Text.Json.Serialization;
using FocusPomodoro.Models;

namespace FocusPomodoro.Services;

public sealed class SettingsService : ISettingsService
{
    public const string FileName = "settings.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly PomodoroSettings _settings;
    private readonly string _filePath;

    public SettingsService(PomodoroSettings settings, string filePath)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    }

    public PomodoroSettings Current => _settings;

    public event EventHandler? SettingsChanged;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            _settings.ResetToDefaults();
            await WriteAsync(cancellationToken).ConfigureAwait(false);
            RaiseSettingsChanged();
            return;
        }

        try
        {
            await using var stream = File.OpenRead(_filePath);
            var loaded = await JsonSerializer
                .DeserializeAsync<PomodoroSettings>(stream, JsonOptions, cancellationToken)
                .ConfigureAwait(false);

            if (loaded is null)
            {
                await RestoreDefaultsAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            _settings.CopyFrom(loaded);
        }
        catch (JsonException)
        {
            await RestoreDefaultsAsync(cancellationToken).ConfigureAwait(false);
            return;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _settings.ResetToDefaults();
        }

        RaiseSettingsChanged();
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        await WriteAsync(cancellationToken).ConfigureAwait(false);
        RaiseSettingsChanged();
    }

    private async Task RestoreDefaultsAsync(CancellationToken cancellationToken)
    {
        _settings.ResetToDefaults();
        await WriteAsync(cancellationToken).ConfigureAwait(false);
        RaiseSettingsChanged();
    }

    private async Task WriteAsync(CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(_filePath);
        await JsonSerializer
            .SerializeAsync(stream, _settings, JsonOptions, cancellationToken)
            .ConfigureAwait(false);
    }

    private void RaiseSettingsChanged() => SettingsChanged?.Invoke(this, EventArgs.Empty);
}
