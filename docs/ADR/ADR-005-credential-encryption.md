# ADR-005: Credential Encryption — AES-GCM + DPAPI

**Status:** Accepted (2026)

## Context
Connection profiles contain database credentials (username, password, connection strings). Storing them as plain text on disk is unacceptable. Windows provides DPAPI (`CryptProtectData`), which encrypts per-user per-machine, but DPAPI alone is fragile — moving the profile file to another machine or re-imaging loses access.

## Decision
- **Per-profile data key:** AES-256-GCM with a random 256-bit key and 96-bit nonce generated per credential vault.
- **Key protection:** The AES key is sealed with **DPAPI** (`ProtectedData.Protect()`, `DataProtectionScope.CurrentUser`).
- **Storage format:** JSON file under `%LOCALAPPDATA%\JustyBaseLegacy\profiles.json`; the `Password` field and `ConnectionString` fields are ciphertext (base64-encoded nonce + tag + ciphertext).
- **Integrity:** GCM provides authenticated encryption — tampered ciphertext is detected.
- **No master password:** DPAPI handles user authentication transparently; no additional UX burden.

## Consequences
+ No plaintext credentials on disk.
+ Transparent to the user — no extra login prompt.
+ GCM authenticates ciphertext; corruption/tampering detected on decrypt.
+ Encryption/decryption is fast (AES-NI hardware acceleration).
+ Profiles are Windows-user-bound — other accounts cannot decrypt (by DPAPI design).
- Profiles are neither portable nor cross-platform (DPAPI is Windows-only, `net10.0-windows` inline).
- If the user account is deleted, all profiles are unrecoverable.
- DPAPI key rotation is Windows-managed — no application control.