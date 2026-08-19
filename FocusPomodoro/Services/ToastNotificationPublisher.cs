using System.Security;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace FocusPomodoro.Services;

internal static class ToastNotificationPublisher
{
    private const string NotificationTag = "focuspomodoro-phase";

    public static void Register()
    {
        try
        {
            AppNotificationManager.Default.Register();
        }
        catch (Exception)
        {
            // Packaged apps can still show toasts via the classic notifier.
        }
    }

    public static void Show(string title, string message)
    {
        try
        {
            var notification = new AppNotificationBuilder()
                .AddText(title)
                .AddText(message)
                .BuildNotification();
            notification.Tag = NotificationTag;
            AppNotificationManager.Default.Show(notification);
        }
        catch (Exception)
        {
            ShowLegacyToast(title, message);
        }
    }

    private static void ShowLegacyToast(string title, string message)
    {
        var xml = $"""
            <toast>
              <visual>
                <binding template="ToastGeneric">
                  <text>{XmlEscape(title)}</text>
                  <text>{XmlEscape(message)}</text>
                </binding>
              </visual>
            </toast>
            """;

        var document = new XmlDocument();
        document.LoadXml(xml);
        var toast = new ToastNotification(document)
        {
            Tag = NotificationTag
        };
        ToastNotificationManager.CreateToastNotifier().Show(toast);
    }

    private static string XmlEscape(string value) => SecurityElement.Escape(value) ?? string.Empty;
}
