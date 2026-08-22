using CommunityToolkit.Mvvm.ComponentModel;
using FocusPomodoro.Helpers;
using FocusPomodoro.Services;

namespace FocusPomodoro.ViewModels;

public sealed class HistoryDayItem
{
    public HistoryDayItem(string header, IReadOnlyList<string> lines)
    {
        Header = header;
        Lines = lines;
    }

    public string Header { get; }
    public IReadOnlyList<string> Lines { get; }
}

public partial class HistoryViewModel : ObservableObject
{
    private readonly IHistoryStore _store;
    private readonly TimeZoneInfo _timeZone;

    public HistoryViewModel(IHistoryStore store, TimeZoneInfo? timeZone = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _timeZone = timeZone ?? TimeZoneInfo.Local;
    }

    [ObservableProperty]
    public partial IReadOnlyList<HistoryDayItem> Days { get; set; } = [];

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var logs = await _store.GetLogsAsync(cancellationToken).ConfigureAwait(false);
            var groups = DailyHistory.GroupByLocalDay(logs, _timeZone);
            Days = groups
                .Select(group => new HistoryDayItem(
                    HistoryPresentation.DayHeader(group),
                    group.Entries.Select(log => HistoryPresentation.Line(log, _timeZone)).ToArray()))
                .ToArray();
        }
        catch (Exception)
        {
            Days = [];
        }
    }
}
