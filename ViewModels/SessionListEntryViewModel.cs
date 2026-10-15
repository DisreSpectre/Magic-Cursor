// STUB: replaced in Phase 3
using CommunityToolkit.Mvvm.ComponentModel;

namespace MagicCursor.ViewModels;

public partial class SessionListEntryViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isHeader;

    [ObservableProperty]
    private string _headerText = string.Empty;

    [ObservableProperty]
    private string _sessionId = string.Empty;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _dateText = string.Empty;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isRenaming;
}
