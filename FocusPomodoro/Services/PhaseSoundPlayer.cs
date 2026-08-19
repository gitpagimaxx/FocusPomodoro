using Windows.Media.Core;
using Windows.Media.Playback;

namespace FocusPomodoro.Services;

internal static class PhaseSoundPlayer
{
    private const string AssetPath = "ms-appx:///Assets/Sounds/phase-complete.wav";
    private const string SystemSoundUri = "ms-winsoundevent:Notification.Default";

    private static MediaPlayer? _player;
    private static bool _fallbackArmed;

    public static void Play()
    {
        var player = _player ??= CreatePlayer();
        player.PlaybackSession.Position = TimeSpan.Zero;
        player.Play();
    }

    public static void Dispose()
    {
        if (_player is null)
        {
            return;
        }

        _player.MediaFailed -= OnMediaFailed;
        _player.Dispose();
        _player = null;
        _fallbackArmed = false;
    }

    private static MediaPlayer CreatePlayer()
    {
        var player = new MediaPlayer
        {
            AudioCategory = MediaPlayerAudioCategory.SoundEffects,
            Volume = 0.35,
            IsLoopingEnabled = false,
            Source = MediaSource.CreateFromUri(new Uri(AssetPath))
        };
        player.MediaFailed += OnMediaFailed;
        return player;
    }

    private static void OnMediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
    {
        if (_fallbackArmed)
        {
            return;
        }

        _fallbackArmed = true;
        sender.Source = MediaSource.CreateFromUri(new Uri(SystemSoundUri));
        sender.Play();
    }
}
