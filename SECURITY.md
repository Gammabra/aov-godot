# Security Policy

## Scope

This policy covers source code, assets, CI/CD, packaged builds, distribution channels, multiplayer backends, and project Infrastructure for **\[PROJECT\_NAME]**.

## Supported Versions

Only the versions listed below receive security fixes.

|              Version |    Status   | Notes                          |
| -------------------: | :---------: | ------------------------------ |
| | | |
| | | |
| | | |

> We do not have any versions released yet. Update this table on each release.

## Reporting a Vulnerability

* **Email:** security@ashes-of-velsingrad.com (or **contact@ashes-of-velsingrad.com**)
* **Response window:** We aim to triage within **3 business days**.
* **Disclosure:** We follow coordinated disclosure. Please do not publicly disclose before a fix is available and users can update.

### What to Include

* Affected version/build and platform(s)
* Reproduction steps or proof-of-concept
* Expected vs. actual behavior
* Impact assessment (e.g., RCE, data leak, account takeover)
* Any logs, crash dumps, or screenshots

## Severity & SLAs (guideline)

| Severity | Examples                                        | Target timeline                    |
| -------- | ----------------------------------------------- | ---------------------------------- |
| Critical | RCE, supply-chain compromise, private key leak  | Hotfix ASAP, public advisory ≤ 72h |
| High     | Auth bypass, sensitive data exposure            | Fix in next patch ≤ 14 days        |
| Medium   | DoS, privilege escalation requiring user action | Fix in next minor ≤ 30 days        |
| Low      | Info leak, minor hardening gaps                 | Next scheduled release             |

Severity uses CVSS v3.1+ as reference.

## Coordinated Disclosure Process

1. Acknowledge report and assign CVSS.
2. Reproduce, confirm scope, and prepare fix/mitigation.
3. Notify ecosystem stakeholders if relevant (stores, server ops).
4. Release patched versions + security advisory (CHANGELOG + GitHub/GitLab advisory).
5. Credit reporter (opt-in) and request retest.

## Secure Development Requirements

### Code & Project

* Prefer **principle of least privilege** for nodes, singletons (autoloads), and OS APIs.
* Treat **exported builds as untrusted**: PCK/PAK contents can be inspected; do not ship secrets.
* Input validation on all external data: files, network packets, savegames, mod data, JSON, config.
* **Never trust client** in multiplayer; validate on authoritative server.
* Use **HTTPS/WSS** only; validate certificates; pin where feasible.
* Cryptography: use well-reviewed libs/APIs; avoid home-grown crypto.
* Disable debug-only features in release (remote scene tree, debug draws, verbose logs).
* Use **Content Security Policy** equivalents for web exports via hosting config.

### Godot-Specific Hardening

* **Export presets**:

  * Uncheck *Debug* features for release exports.
  * Remove editor remotes and any tooling remnants.
  * Strip symbols when possible; include only required resources.
* **Secrets**: do not embed API keys or tokens in project; load at runtime from secure storage/server.
* **File Access**: restrict to `user://` where possible; validate file paths; avoid arbitrary `res://` writes.
* **Networking**: prefer authenticated sessions; sign/verify messages where needed; throttle to mitigate spam/DoS.
* **Multiplayer**: if using ENet or WebSocket/WebRTC, ensure transport is encrypted (e.g., WSS/TLS) and messages are authenticated; never trust client state.
* **Autoloads**: audit global singletons for unintended capabilities.
* **Android/iOS**: request only required permissions; review manifests/entitlements per release.
* **Desktop**: consider sandboxing with platform tools (Flatpak, AppSandbox, Gatekeeper/Notarization, AppArmor/Snap) where applicable.
* **Web**: host over HTTPS; set proper headers (CSP, COOP/COEP if using threads); beware localStorage/sessionStorage exposure.

### Dependencies & Supply Chain

* Pin engine version (e.g., Godot 4.x.y) and third-party libs; track hashes.
* Use lockfiles for C# (NuGet) or any package managers used by tooling.
* Verify third-party assets/scripts; document sources and licenses.
* CI: verify checksums for downloaded tools; use minimal, pinned runners/containers.
* Sign releases where platform supports it (Windows Authenticode, macOS notarization, Android signing, etc.).

### Build, CI/CD & Releases

* Protected branches; code review required for privileged areas (networking, deserialization, crypto, auth).
* Secrets in CI via platform secret store; **no secrets in repo**.
* Reproducible/exportable builds where feasible; archive SBOM (Software Bill of Materials).
* Publish **security advisories** with CVE (if applicable), impact, affected versions, and upgrade guidance.

### Data, Privacy & Logging

* Collect minimal telemetry (if any); document what is collected and why.
* Store user data only in `user://` (or platform-appropriate sandbox) with access controls.
* Do not log secrets or PII; enable log redaction filters.
* Provide data deletion/reset options.

## Testing & Review Checklist

* [ ] Fuzz/negative tests for savegame and config parsers
* [ ] Unit tests for input validation and auth flows
* [ ] Multiplayer: server authoritative checks; anti-cheat/anti-spam rate limits
* [ ] Deserialization safety: avoid executing data as code; whitelist formats
* [ ] Verify no debug remotes or backdoors in release exports
* [ ] TLS/WSS certificate validation covered in tests
* [ ] Static analysis (GDScript/C# linters) in CI
* [ ] Dependency audit (NuGet/third-party libs, asset licenses)
* [ ] Platform permission review (Android/iOS manifests)
* [ ] Web export headers validated (CSP, HSTS if applicable)

## Vulnerability Handling

* **Branching:** create private fix branch; restrict visibility until release.
* **Advisory:** include mitigation steps and version matrix.
* **Backports:** backport to supported minor lines per table above.
* **Credits:** acknowledge reporters (handle anonymity requests).

## Incident Response (IR)

* IR contact: **security@ashes-of-velsingrad.com**
* Immediate steps: freeze releases, rotate credentials, invalidate tokens, audit downloads.
* Postmortem: publish timeline, root cause, and preventative actions.

## Contact & Keys

* Primary: **security@ashes-of-velsingrad.com**
* Backup: **contact@ashes-of-velsingrad.com**

## Legal

* By reporting, you agree to act in good faith, avoid privacy violations, and follow applicable laws. We will not pursue legal action against good-faith research within this policy.

## Acknowledgements

Thanks to all security researchers and contributors who help keep **Ashes of Velsingrad** safe.
