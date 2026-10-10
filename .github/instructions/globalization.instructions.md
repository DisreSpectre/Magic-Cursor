---
description: 'Globalization for user-facing strings, resources, culture-aware formatting'
applyTo: '**/*.cs, **/*.xaml, **/*.resw'
---

# Globalization & Localization

## Current state

No `.resw` files yet — existing strings are hard-coded. Create `Strings/en-us/Resources.resw` when adding the first new localized string. Migrate existing strings opportunistically when touching surrounding code.

## Rules

- **All new user-facing strings** go in `.resw` — never hard-code new UI text.
- XAML: `x:Uid="Key"` with matching `.resw` entry `Key.Text`.
- C#: `new ResourceLoader().GetString("Key")`.
- Format dates/times/numbers with `CultureInfo.CurrentCulture` or `DateTimeFormatter` — no hard-coded formats.
- Use format placeholders (`{0}`), not concatenation, for translated sentences.
- Allow ~30–40% text expansion in layouts.

## Voice / Chat

- Localize: voice statuses ("Listening…", "Muted"), chat role labels, timestamps, buttons, placeholders, error banners.
- **AI output is data** — never localize LLM response text via `.resw`.

## Validation

- New strings exist in `.resw`; new XAML uses `x:Uid`.
- Timestamps and statuses respect system culture.

## Must Read & Research

| # | Reference |
|---|---|
| 1 | [Globalize your WinUI app](https://learn.microsoft.com/en-us/windows/apps/design/globalizing/guidelines-and-checklist-for-globalizing-your-app) |
| 2 | [Resource Management System](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/mrtcore/localize-strings) |
| 3 | [Localization with x:Uid](https://learn.microsoft.com/en-us/windows/apps/develop/ui-input/localizing-strings) |
