# ApexTracker

A community-built Windows app that saves Apex Legends match results using
local screen capture and OCR.

## Project goals

- Record match results locally: placement, kills, assists, damage, and other
  fields that can be reliably recognized from result screens.
- Keep the project transparent and auditable.
- Invite developers and testers to improve OCR accuracy, performance, privacy,
  compatibility, and fair-play safety.

## Safety and fair-play boundary

ApexTracker is designed to be non-invasive:

- No game-memory reading or writing
- No DLL injection or process modification
- No input automation, macros, recoil control, or gameplay assistance
- No game-file modification
- OCR is performed locally from captured result screens

This project is not affiliated with, endorsed by, or whitelisted by EA or
Respawn. No third-party software can promise zero account-risk. Please review
the source code, test carefully, and follow EA's rules at all times.

## How to help

We welcome contributions for:

- OCR accuracy across languages, resolutions, HDR, and UI layouts
- Performance profiling and lower-overhead capture strategies
- Privacy and security audits
- Accessibility and localization
- Installer, documentation, and reproducible test reports
- Compatibility testing that does not bypass game protections or violate rules
