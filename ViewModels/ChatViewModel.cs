// STUB: replaced in Phase 3
using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace MagicCursor.ViewModels;

public enum ChatMode { Text, Voice }
public enum VoiceState { Idle, Connecting, Listening, Speaking, Reconnecting, Error }
public enum VoiceInputMode { OpenMic, PushToTalk, AutoMuteWhileSpeaking }

public partial class ChatViewModel : ObservableObject
{
    // Header
    [ObservableProperty]
    private string _title = "Magic Cursor";

    [ObservableProperty]
    private string _modelBadge = "Gemini 2.5 Flash";

    [ObservableProperty]
    private bool _isHistoryOpen;

    // History
    [ObservableProperty]
    private ObservableCollection<SessionListEntryViewModel> _sessionList = new();

    [ObservableProperty]
    private bool _isSessionListEmpty;

    // Thread
    [ObservableProperty]
    private ObservableCollection<ChatMessageViewModel> _messages = new();

    [ObservableProperty]
    private bool _isThreadEmpty;

    [ObservableProperty]
    private bool _isBusy;

    // Input
    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    private bool _canSend = true;

    [ObservableProperty]
    private bool _hasStagedImage;

    [ObservableProperty]
    private ImageSource? _stagedThumbnail;

    [ObservableProperty]
    private string _stagedLabel = string.Empty;

    // Errors / notices
    [ObservableProperty]
    private bool _isErrorVisible;

    [ObservableProperty]
    private string _errorText = string.Empty;

    [ObservableProperty]
    private bool _isHeadphoneNoticeVisible;

    // Voice
    [ObservableProperty]
    private ChatMode _mode = ChatMode.Text;

    [ObservableProperty]
    private bool _isVoiceMode;

    [ObservableProperty]
    private VoiceState _voiceState = VoiceState.Idle;

    [ObservableProperty]
    private string _voiceStatusText = "Ready";

    [ObservableProperty]
    private VoiceInputMode _voiceInputMode = VoiceInputMode.OpenMic;

    [ObservableProperty]
    private bool _isPushToTalk;

    [ObservableProperty]
    private bool _isMicMuted;

    [ObservableProperty]
    private double _audioLevel;

    // Commands (IRelayCommand / IAsyncRelayCommand)
    public IRelayCommand SendCommand { get; }
    public IRelayCommand CancelGenerationCommand { get; }
    public IRelayCommand NewChatCommand { get; }
    public IRelayCommand ToggleHistoryCommand { get; }
    public IRelayCommand CloseCommand { get; }
    public IRelayCommand RemoveStagedImageCommand { get; }
    public IRelayCommand CaptureRegionCommand { get; }
    public IRelayCommand<SessionListEntryViewModel> SelectSessionCommand { get; }
    public IRelayCommand<SessionListEntryViewModel> RenameSessionCommand { get; }
    public IRelayCommand<SessionListEntryViewModel> DeleteSessionCommand { get; }
    public IRelayCommand<ChatMessageViewModel> CopyMessageCommand { get; }
    public IRelayCommand<ChatMessageViewModel> RegenerateCommand { get; }
    public IRelayCommand<ChatMessageViewModel> PreviewImageCommand { get; }
    public IRelayCommand StartVoiceCommand { get; }
    public IRelayCommand ExitVoiceCommand { get; }
    public IRelayCommand InterruptCommand { get; }
    public IRelayCommand ToggleMuteCommand { get; }
    public IRelayCommand<VoiceInputMode> SetVoiceInputModeCommand { get; }
    public IRelayCommand DismissHeadphoneNoticeCommand { get; }
    public IRelayCommand DismissErrorCommand { get; }

    // Methods called from code-behind
    public void BeginPushToTalk() { }
    public void EndPushToTalk() { }

    // Events
#pragma warning disable CS0067
    public event EventHandler? ScrollToBottomRequested;
    public event EventHandler? FocusInputRequested;
#pragma warning restore CS0067

    public ChatViewModel()
    {
        // No-op stub commands
        SendCommand = new RelayCommand(() => { });
        CancelGenerationCommand = new RelayCommand(() => { });
        NewChatCommand = new RelayCommand(() => { });
        ToggleHistoryCommand = new RelayCommand(() => IsHistoryOpen = !IsHistoryOpen);
        CloseCommand = new RelayCommand(() => { });
        RemoveStagedImageCommand = new RelayCommand(() => { HasStagedImage = false; StagedThumbnail = null; StagedLabel = string.Empty; });
        CaptureRegionCommand = new RelayCommand(() => { });
        SelectSessionCommand = new RelayCommand<SessionListEntryViewModel>(_ => { });
        RenameSessionCommand = new RelayCommand<SessionListEntryViewModel>(_ => { });
        DeleteSessionCommand = new RelayCommand<SessionListEntryViewModel>(_ => { });
        CopyMessageCommand = new RelayCommand<ChatMessageViewModel>(_ => { });
        RegenerateCommand = new RelayCommand<ChatMessageViewModel>(_ => { });
        PreviewImageCommand = new RelayCommand<ChatMessageViewModel>(_ => { });
        StartVoiceCommand = new RelayCommand(() => { Mode = ChatMode.Voice; IsVoiceMode = true; });
        ExitVoiceCommand = new RelayCommand(() => { Mode = ChatMode.Text; IsVoiceMode = false; });
        InterruptCommand = new RelayCommand(() => { });
        ToggleMuteCommand = new RelayCommand(() => IsMicMuted = !IsMicMuted);
        SetVoiceInputModeCommand = new RelayCommand<VoiceInputMode>(mode => { VoiceInputMode = mode; IsPushToTalk = mode == VoiceInputMode.PushToTalk; });
        DismissHeadphoneNoticeCommand = new RelayCommand(() => IsHeadphoneNoticeVisible = false);
        DismissErrorCommand = new RelayCommand(() => IsErrorVisible = false);

#if DEBUG
        PopulateSampleData();
#endif
    }

#if DEBUG
    private void PopulateSampleData()
    {
        Title = "Magic Cursor";
        ModelBadge = "Gemini 2.5 Flash";
        IsHistoryOpen = false;

        // 3 sessions across date groups
        SessionList = new ObservableCollection<SessionListEntryViewModel>
        {
            new SessionListEntryViewModel { IsHeader = true, HeaderText = "Today" },
            new SessionListEntryViewModel { SessionId = "s1", Title = "Window docking implementation", DateText = "14:20", IsSelected = true },
            new SessionListEntryViewModel { IsHeader = true, HeaderText = "Previous 7 days" },
            new SessionListEntryViewModel { SessionId = "s2", Title = "Nord palette contrast check", DateText = "Yesterday" },
            new SessionListEntryViewModel { IsHeader = true, HeaderText = "Older" },
            new SessionListEntryViewModel { SessionId = "s3", Title = "Screen capture pipeline setup", DateText = "Oct 4" }
        };
        IsSessionListEmpty = false;

        // Several messages including a code block, one with thumbnail
        Messages = new ObservableCollection<ChatMessageViewModel>
        {
            new ChatMessageViewModel
            {
                Id = "m1",
                IsUser = true,
                Text = "Can you help me format this code snippet?",
                TimestampText = "14:20"
            },
            new ChatMessageViewModel
            {
                Id = "m2",
                IsUser = false,
                Text = "Sure! Here is the clean implementation in C#:\n\n```csharp\npublic void SayHello()\n{\n    Console.WriteLine(\"Hello, Nord!\");\n}\n```\n\nLet me know if you need any adjustments.",
                TimestampText = "14:21"
            },
            new ChatMessageViewModel
            {
                Id = "m3",
                IsUser = true,
                Text = "I attached the screen capture for reference.",
                TimestampText = "14:22",
                HasThumbnail = true,
                ThumbnailLabel = "capture_screen.png",
                Thumbnail = new BitmapImage(new Uri("ms-appx:///Assets/Square44x44Logo.scale-200.png"))
            },
            new ChatMessageViewModel
            {
                Id = "m4",
                IsUser = false,
                Text = "I see the capture. The visual elements align with the design system.",
                TimestampText = "14:22",
                IsLastAssistant = true
            }
        };
        IsThreadEmpty = false;

        // Staged image
        HasStagedImage = true;
        StagedLabel = "selection_rect.png";
        StagedThumbnail = new BitmapImage(new Uri("ms-appx:///Assets/Square44x44Logo.scale-200.png"));

        VoiceStatusText = "Ready";
        AudioLevel = 0.4;
    }
#endif
}
