---
description: 'Accessibility for interactive controls, keyboard nav, screen readers, contrast'
applyTo: '**/*.cs, **/*.xaml'
---

# Accessibility

Applies to **new and modified UI**. Existing `MainPage.xaml` controls are exempt until touched.

## Rules

- New interactive controls need `AutomationProperties.Name` or `LabeledBy`. Key controls need stable `AutomationId`.
- Use semantic controls (`Button`, `HyperlinkButton`, `ListView`), not clickable `Border`/`Grid`.
- Keyboard: logical `TabIndex`, `AccessKey` for frequent actions, `KeyboardAccelerator` for shortcuts.
- Contrast: 4.5:1 normal text, 3:1 large. Test High Contrast.
- Images: `AutomationProperties.Name` describing purpose, or `AccessibilityView="Raw"` for decorative.
- Don't rely on colour alone. Don't use `Visibility.Collapsed` to hide from screen readers — use `AccessibilityView`.

## Voice / Chat

- **Gesture alternative**: any gesture-only activation (mouse shake) must have a keyboard hotkey alternative.
- **Voice output**: provide visual captions/transcripts. Announce state changes ("Listening", "Muted") via live regions.
- **Chat**: message list must be keyboard navigable. Announce new messages without stealing focus. Chat controls (input, send, voice toggle, stop, copy) need `AutomationProperties.Name` and accelerators (Enter to send, Shift+Enter newline, Escape cancel).

## Validation

- Tab through new/changed UI. Verify hotkey works without mouse.
- Run Narrator and Accessibility Insights. Test High Contrast.

## Must Read & Research

| # | Reference |
|---|---|
| 1 | [Accessibility in WinUI](https://learn.microsoft.com/en-us/windows/apps/design/accessibility/accessibility) |
| 2 | [AutomationProperties](https://learn.microsoft.com/en-us/windows/apps/design/accessibility/basic-accessibility-information) |
| 3 | [Accessibility Insights](https://accessibilityinsights.io/docs/windows/overview/) |
| 4 | [Keyboard Accessibility](https://learn.microsoft.com/en-us/windows/apps/design/accessibility/keyboard-accessibility) |
| 5 | [High Contrast Themes](https://learn.microsoft.com/en-us/windows/apps/design/accessibility/high-contrast-themes) |
