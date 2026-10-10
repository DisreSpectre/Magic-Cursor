---
description: 'DRY, KISS, SOLID, YAGNI enforced in every change'
applyTo: '**/*.cs, **/*.xaml'
---

# Design Principles

Apply in every change. Favour simplicity over cleverness.

## Pragmatic exceptions

- **SRP**: `MainPage.xaml.cs` is a god class (overlay, capture, OCR, AI, hooks, animation). **Do not add new responsibilities to it.** New features — especially **voice mode** and **chat** — must live in separate classes (`VoiceService`, `ChatViewModel`, `SpeechRecognitionService`, `ConversationService`).
- **DIP**: static services (`ConfigService`, `LensService`, `Log`) and manual `new GeminiService()` exist. For new features, define interfaces (`IVoiceService`, `IChatService`) and prefer constructor injection. Do not add new global static state.

## DRY

Search before writing. Extract shared logic. Refactor duplication in the same change.

## KISS

Simplest approach. Methods ≤ ~30 lines. One responsibility per class. No deep inheritance, no unnecessary abstraction, no clever LINQ where `foreach` is clearer.

## SOLID

- **SRP**: one reason to change per class. Voice = separate recognition/TTS services. Chat = separate ViewModel/service.
- **OCP**: extend via interfaces. Design voice/chat so speech engines and LLM backends can be swapped without UI changes.
- **LSP**: honour base contracts. Never throw `NotImplementedException` in overrides.
- **ISP**: small focused interfaces (`IVoiceRecognitionService`, `ISpeechSynthesisService`, `IChatHistoryStore` — not one monolithic `IAIService`).
- **DIP**: depend on abstractions. Constructor injection for new features.

## YAGNI

Only implement what's requested. No speculative parameters, interfaces, or config options.

## Validation

- Every new class has one reason to change.
- No voice/chat logic in `MainPage.xaml.cs`.
- Every line traces to a requirement.

## Must Read & Research

| # | Reference |
|---|---|
| 1 | [SOLID in C#](https://learn.microsoft.com/en-us/archive/msdn-magazine/2014/may/csharp-best-practices-dangers-of-violating-solid-principles-in-csharp) |
| 2 | [.NET Design Guidelines](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/) |
| 3 | [Clean Code Summary](https://gist.github.com/wojteklu/73c6914cc446146b8b533c0988cf8d29) |
