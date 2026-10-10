---
description: 'Performance for data binding, layout, threading, virtualization'
applyTo: '**/*.cs, **/*.xaml'
---

# Performance

## Current constraints

- Overlay `ScreenCapture` (BitBlt + OCR) runs on the UI thread — known legacy issue.
- Overlay uses no data binding — acceptable for the canvas. **All new UI (chat, settings, voice indicators) must use `x:Bind`.**
- New performance-sensitive features (**chat streaming**, **voice processing**) must not block the UI thread.

## Rules

- Use `x:Bind` for new bindings; `x:Load` to defer heavy controls (voice visualizers, markdown renderers).
- Virtualize chat history with `ListView` or `ItemsRepeater` — never a `StackPanel` in a `ScrollViewer` for message lists.
- **Voice**: audio capture, recognition, and TTS decoding on background threads. Post only transcripts to UI via `DispatcherQueue.TryEnqueue`.
- **Chat**: stream tokens via `IAsyncEnumerable<string>` or callbacks. Throttle UI updates (~16–33ms batching) to avoid flooding `DispatcherQueue`.
- Cache reusable brushes, geometries, regexes (as `MainPage.xaml.cs` already does).
- Minimize visual tree depth in message templates.

## Anti-patterns

- `.Result` / `.GetAwaiter().GetResult()` / `Thread.Sleep` on UI thread.
- Updating `TextBlock.Text` per streamed token without throttling.
- Non-virtualized message lists.
- Allocating brushes/converters in tight loops.

## Validation

- Chat scrolls smoothly with 50+ messages.
- UI thread not pegged during streaming or speech recognition.
- New views use `x:Bind`.

## Must Read & Research

| # | Reference | When |
|---|---|---|
| 1 | [WinUI 3 Performance](https://learn.microsoft.com/en-us/windows/apps/performance/) | Rendering, threading, data |
| 2 | [x:Bind](https://learn.microsoft.com/en-us/windows/uwp/xaml-platform/x-bind-markup-extension) | Bindings |
| 3 | [x:Load](https://learn.microsoft.com/en-us/windows/uwp/xaml-platform/x-load-attribute) | Deferred loading |
| 4 | [Optimize XAML layout](https://learn.microsoft.com/en-us/windows/apps/performance/optimize-xaml-layout) | Panels, tree depth |
| 5 | [ListView optimization](https://learn.microsoft.com/en-us/windows/apps/performance/optimize-listview) | Lists, `ItemsRepeater` |
