---
description: 'Secrets, input validation, permissions, secure coding'
applyTo: '**/*.cs, **/*.appxmanifest'
---

# Security

## Current state

- API key is stored plaintext at `%LOCALAPPDATA%\MagicCursor\config.json`.
- **New secrets must use `PasswordVault` or Windows Credential Manager.** Migrate the existing key opportunistically when refactoring settings.
- Never hard-code secrets or commit them to git.

## Rules

- Validate/sanitize all external input. Sanitize markdown/HTML to prevent injection and malicious URI schemes.
- Least privilege in `Package.appxmanifest`.
- HTTPS + TLS validation always. Keep dependencies updated (`dotnet list package --outdated`).
- Code signing: use `winapp` CLI. Production releases signed by a trusted CA — never ship the dev cert.
- Never log PII, tokens, passwords, raw audio, or chat message bodies. Log only status codes, error types, metrics.

## Voice

- Declare `<DeviceCapability Name="microphone" />` in `Package.appxmanifest`. Request permission only when voice mode activates.
- Never persist raw microphone audio unless explicitly requested by the user.
- Transmit audio only over TLS when using cloud speech services.

## Chat

- Never log prompt or response content.
- If persisting history, use `ApplicationData.Current.LocalFolder` or DPAPI-protected storage.

## Anti-patterns

- Secrets in source control. Disabling TLS. `Process.Start` with unsanitized input. Silent `catch (Exception)`.

## Validation

- Grep for `password`, `apikey`, `secret`, `connectionstring` in `.cs`.
- Verify audio and chat content excluded from `Log.cs` output.
- `Package.appxmanifest` contains only required capabilities.

## Must Read & Research

| # | Reference | When |
|---|---|---|
| 1 | [.NET Security](https://learn.microsoft.com/en-us/dotnet/standard/security/) | Credentials, sensitive data |
| 2 | [Secure coding guidelines](https://learn.microsoft.com/en-us/dotnet/standard/security/secure-coding-guidelines) | Input validation, exceptions |
| 3 | [MSIX Security](https://learn.microsoft.com/en-us/windows/msix/msix-container) | Packaging, signing |
| 4 | [App capabilities](https://learn.microsoft.com/en-us/windows/uwp/packaging/app-capability-declarations) | Permissions |
