using System;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.System;
using Windows.UI.ViewManagement;
using MagicCursor.ViewModels;
using VoiceState = MagicCursor.ViewModels.VoiceState;

namespace MagicCursor.Views;

public sealed partial class ChatView : UserControl
{
    // Obtained via DI
    public ChatViewModel ViewModel { get; }

    public ChatView()
    {
        InitializeComponent();

        // Wire ViewModel via App.Services
        ViewModel = App.Services.GetRequiredService<ChatViewModel>();
        this.DataContext = ViewModel;

        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        ViewModel.ScrollToBottomRequested += ViewModel_ScrollToBottomRequested;
        ViewModel.FocusInputRequested += ViewModel_FocusInputRequested;

        this.Loaded += ChatView_Loaded;
    }

    private void ChatView_Loaded(object sender, RoutedEventArgs e)
    {
        PlayEnter();
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ViewModel.IsVoiceMode):
            case nameof(ViewModel.Mode):
                VisualStateManager.GoToState(this, ViewModel.IsVoiceMode ? "VoiceState" : "TextState", true);
                break;

            case nameof(ViewModel.IsHistoryOpen):
                VisualStateManager.GoToState(this, ViewModel.IsHistoryOpen ? "HistoryOpen" : "HistoryClosed", true);
                break;

            case nameof(ViewModel.VoiceState):
                string stateName = ViewModel.VoiceState switch
                {
                    MagicCursor.ViewModels.VoiceState.Connecting => "ConnectingState",
                    MagicCursor.ViewModels.VoiceState.Listening => "ListeningState",
                    MagicCursor.ViewModels.VoiceState.Speaking => "SpeakingState",
                    MagicCursor.ViewModels.VoiceState.Reconnecting => "ReconnectingState",
                    MagicCursor.ViewModels.VoiceState.Error => "ErrorState",
                    _ => ViewModel.IsMicMuted ? "MutedState" : "IdleState"
                };
                VisualStateManager.GoToState(this, stateName, true);
                break;

            case nameof(ViewModel.AudioLevel):
                UpdateWaveform(ViewModel.AudioLevel);
                break;
        }
    }

    private void ViewModel_ScrollToBottomRequested(object? sender, EventArgs e)
    {
        if (ViewModel.Messages.Count > 0)
        {
            MessagesListView.ScrollIntoView(ViewModel.Messages[^1]);
        }
    }

    private void ViewModel_FocusInputRequested(object? sender, EventArgs e)
    {
        ChatInputBox.Focus(FocusState.Programmatic);
    }

    #region Animations & Waveform

    public void PlayEnter()
    {
        var settings = new UISettings();
        if (!settings.AnimationsEnabled)
        {
            ContentRootTransform.X = 0;
            ContentRoot.Opacity = 1;
            return;
        }

        var sb = new Storyboard();
        var transAnim = new DoubleAnimation
        {
            From = 40,
            To = 0,
            Duration = new Duration(TimeSpan.FromMilliseconds(200)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(transAnim, ContentRootTransform);
        Storyboard.SetTargetProperty(transAnim, "X");

        var fadeAnim = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = new Duration(TimeSpan.FromMilliseconds(200))
        };
        Storyboard.SetTarget(fadeAnim, ContentRoot);
        Storyboard.SetTargetProperty(fadeAnim, "Opacity");

        sb.Children.Add(transAnim);
        sb.Children.Add(fadeAnim);
        sb.Begin();
    }

    public void PlayExit(Action completed)
    {
        var settings = new UISettings();
        if (!settings.AnimationsEnabled)
        {
            completed?.Invoke();
            return;
        }

        var sb = new Storyboard();
        var transAnim = new DoubleAnimation
        {
            From = 0,
            To = 40,
            Duration = new Duration(TimeSpan.FromMilliseconds(150)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        Storyboard.SetTarget(transAnim, ContentRootTransform);
        Storyboard.SetTargetProperty(transAnim, "X");

        var fadeAnim = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = new Duration(TimeSpan.FromMilliseconds(150))
        };
        Storyboard.SetTarget(fadeAnim, ContentRoot);
        Storyboard.SetTargetProperty(fadeAnim, "Opacity");

        sb.Children.Add(transAnim);
        sb.Children.Add(fadeAnim);
        sb.Completed += (_, _) => completed?.Invoke();
        sb.Begin();
    }

    public void UpdateWaveform(double level)
    {
        level = Math.Clamp(level, 0.0, 1.0);
        WaveScale1.ScaleY = Math.Clamp(0.2 + level * 0.4, 0.1, 1.0);
        WaveScale2.ScaleY = Math.Clamp(0.2 + level * 0.8, 0.1, 1.0);
        WaveScale3.ScaleY = Math.Clamp(0.2 + level * 1.2, 0.1, 1.0);
        WaveScale4.ScaleY = Math.Clamp(0.2 + level * 1.4, 0.1, 1.0);
        WaveScale5.ScaleY = Math.Clamp(0.2 + level * 1.1, 0.1, 1.0);
        WaveScale6.ScaleY = Math.Clamp(0.2 + level * 0.7, 0.1, 1.0);
        WaveScale7.ScaleY = Math.Clamp(0.2 + level * 0.3, 0.1, 1.0);
    }

    #endregion

    #region Input & History Handlers

    private void ChatInputBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter)
        {
            var isShiftDown = (Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift) &
                               Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;

            if (!isShiftDown)
            {
                e.Handled = true;
                if (ViewModel.CanSend && ViewModel.SendCommand.CanExecute(null))
                {
                    ViewModel.SendCommand.Execute(null);
                }
            }
        }
    }

    private void SessionListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is SessionListEntryViewModel entry && !entry.IsHeader)
        {
            ViewModel.SelectSessionCommand.Execute(entry);
            ViewModel.IsHistoryOpen = false;
        }
    }

    private void RenameTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter && sender is TextBox tb && tb.DataContext is SessionListEntryViewModel entry)
        {
            e.Handled = true;
            entry.IsRenaming = false;
            ViewModel.RenameSessionCommand.Execute(entry);
        }
        else if (e.Key == VirtualKey.Escape && sender is TextBox tbEsc && tbEsc.DataContext is SessionListEntryViewModel entryEsc)
        {
            e.Handled = true;
            entryEsc.IsRenaming = false;
        }
    }

    private void RenameTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is SessionListEntryViewModel entry)
        {
            entry.IsRenaming = false;
            ViewModel.RenameSessionCommand.Execute(entry);
        }
    }

    private void HistoryScrim_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        ViewModel.IsHistoryOpen = false;
    }

    private void PushToTalkButton_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        ViewModel.BeginPushToTalk();
    }

    private void PushToTalkButton_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        ViewModel.EndPushToTalk();
    }

    private void PushToTalkButton_PointerCanceled(object sender, PointerRoutedEventArgs e)
    {
        ViewModel.EndPushToTalk();
    }

    private void PushToTalkButton_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        ViewModel.EndPushToTalk();
    }

    private void PushToTalkButton_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Space)
        {
            e.Handled = true;
            ViewModel.BeginPushToTalk();
        }
    }

    private void PushToTalkButton_KeyUp(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Space)
        {
            e.Handled = true;
            ViewModel.EndPushToTalk();
        }
    }

    private void RenameSessionButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is SessionListEntryViewModel entry)
        {
            entry.IsRenaming = true;
        }
    }

    private void DeleteSessionButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is SessionListEntryViewModel entry)
        {
            ViewModel.DeleteSessionCommand.Execute(entry);
        }
    }

    private void ThumbnailButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is ChatMessageViewModel msg)
        {
            ViewModel.PreviewImageCommand.Execute(msg);
        }
    }

    private void CopyMessageButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is ChatMessageViewModel msg)
        {
            ViewModel.CopyMessageCommand.Execute(msg);
        }
    }

    private void RegenerateButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is ChatMessageViewModel msg)
        {
            ViewModel.RegenerateCommand.Execute(msg);
        }
    }

    private void ErrorInfoBar_CloseButtonClick(InfoBar sender, object args)
    {
        ViewModel.DismissErrorCommand.Execute(null);
    }

    private void HeadphoneNoticeInfoBar_CloseButtonClick(InfoBar sender, object args)
    {
        ViewModel.DismissHeadphoneNoticeCommand.Execute(null);
    }

    private void VoiceModeOpenMic_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SetVoiceInputModeCommand.Execute(VoiceInputMode.OpenMic);
    }

    private void VoiceModePtt_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SetVoiceInputModeCommand.Execute(VoiceInputMode.PushToTalk);
    }

    private void VoiceModeAutoMute_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SetVoiceInputModeCommand.Execute(VoiceInputMode.AutoMuteWhileSpeaking);
    }

    private void NewChatAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        ViewModel.NewChatCommand.Execute(null);
    }

    private void HistoryAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        ViewModel.ToggleHistoryCommand.Execute(null);
    }

    private void EscapeAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        if (ViewModel.IsHistoryOpen)
        {
            ViewModel.IsHistoryOpen = false;
        }
        else
        {
            ViewModel.CloseCommand.Execute(null);
        }
    }

    #endregion

    #region Function Binding Helpers for XAML

    public static Visibility GetMicLiveVisibility(bool isVoiceMode, bool isMicMuted) =>
        (isVoiceMode && !isMicMuted) ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility GetAccentBarVisibility(bool isSelected) =>
        isSelected ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility GetNormalVisibility(bool isRenaming) =>
        isRenaming ? Visibility.Collapsed : Visibility.Visible;

    public static Visibility GetRenameVisibility(bool isRenaming) =>
        isRenaming ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility GetUserVisibility(bool isUser) =>
        isUser ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility GetAssistantVisibility(bool isUser) =>
        isUser ? Visibility.Collapsed : Visibility.Visible;

    public static Visibility GetThumbnailVisibility(bool visible) =>
        visible ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility GetTypingIndicatorVisibility(bool isStreaming, string text) =>
        (isStreaming && string.IsNullOrEmpty(text)) ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility GetStreamingProgressVisibility(bool isStreaming, string text) =>
        (isStreaming && !string.IsNullOrEmpty(text)) ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility GetSendButtonVisibility(bool isBusy) =>
        isBusy ? Visibility.Collapsed : Visibility.Visible;

    public static Visibility GetStopButtonVisibility(bool isBusy) =>
        isBusy ? Visibility.Visible : Visibility.Collapsed;

    public static bool GetModeChecked(VoiceInputMode currentMode, int index) =>
        (int)currentMode == index;

    public static string GetMuteGlyph(bool isMuted) =>
        isMuted ? "\uF781" : "\uE720";

    #endregion
}

public class SessionEntryTemplateSelector : DataTemplateSelector
{
    public DataTemplate HeaderTemplate { get; set; } = null!;
    public DataTemplate SessionTemplate { get; set; } = null!;

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is SessionListEntryViewModel entry && entry.IsHeader)
        {
            return HeaderTemplate;
        }
        return SessionTemplate;
    }
}
