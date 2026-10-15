// STUB: replaced in Phase 3
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;

namespace MagicCursor.ViewModels;

public partial class ChatMessageViewModel : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private bool _isUser;

    [ObservableProperty]
    private string _text = string.Empty;

    [ObservableProperty]
    private bool _isStreaming;

    [ObservableProperty]
    private bool _isVoiceTurn;

    [ObservableProperty]
    private bool _isLastAssistant;

    [ObservableProperty]
    private string _timestampText = string.Empty;

    [ObservableProperty]
    private ImageSource? _thumbnail;

    [ObservableProperty]
    private bool _hasThumbnail;

    [ObservableProperty]
    private string _thumbnailLabel = string.Empty;

    [ObservableProperty]
    private bool _isFullImageUnavailable;
}
