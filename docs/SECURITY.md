# Security Policy

The security of FE-BUDDY and its users is important. If you discover a potential security
vulnerability, please report it privately so it can be investigated before it is publicly
disclosed.

## Reporting a Security Vulnerability

**Please do not report security vulnerabilities through public GitHub Issues, Discussions, or
Pull Requests.**

FE-BUDDY has GitHub's **Private Vulnerability Reporting** enabled. To report a vulnerability
privately, use **[Report a vulnerability](https://github.com/Nikolai558/FE-BUDDY/security/advisories/new)**,
or:

1. Go to the FE-BUDDY repository on GitHub.
2. Select the **Security** tab.
3. Select **Advisories**, then **Report a vulnerability**.
4. Fill in and submit the report.

Please include as much as you can:

* The affected FE-BUDDY version (shown at the top of its window).
* A description of the vulnerability.
* Steps to reproduce it.
* The potential security impact.
* Any relevant logs, screenshots, or other supporting information. FE-BUDDY's log is in
  `%APPDATA%\FE-Buddy\Logs` - check it for anything private before attaching it.
* Any known workaround or mitigation.

Reports are reviewed as soon as reasonably possible. Please do not disclose the details publicly
until the report has been investigated and, when necessary, a fix has been released.

## What counts as a security issue

Security issues are problems that could let someone read, change, or run something they should
not. For FE-BUDDY, for example:

* **Credentials** - the GitHub token or other credentials you store in FE-BUDDY (kept in Windows
  Credential Manager) being exposed: written to a log or file, sent to the wrong website, or
  readable by another user.
* **Updates and installing** - anything that could make FE-BUDDY download or run an installer
  that is not an official FE-BUDDY release, or abuse the installer's administrator rights.
* **Files and downloads** - a crafted file (DAT, SCT2, ERAM, alias or settings file) or a crafted
  download (FAA data, News, custom alias files) that makes FE-BUDDY run code, or read or write
  files outside the folders it should.
* **This repository and its release process** - for example a GitHub Actions workflow that could
  leak a secret or publish something it should not.

Everything else - crashes, wrong or missing AIRAC data, parsing problems, update failures,
incorrect output, and feature requests - is a normal bug. Please report it through
[GitHub Issues](https://github.com/Nikolai558/FE-BUDDY/issues). If you are unsure whether an
issue has security implications, report it privately.

## Supported Versions

Security fixes are provided only for supported versions of FE-BUDDY, and they ship in a new
release.

| Version | Supported |
| ------- | --------- |
| 3.0.x, including its pre-releases | :white_check_mark: The latest release only |
| 2.9.x | :warning: Critical security fixes only, until 3.0.0 is released |
| < 2.9.0 | :x: |

FE-BUDDY 2.9.3 is the last planned 2.x release. Please move to FE-BUDDY 3.x when you can: the
in-app updater offers it, or download it from [Releases](https://github.com/Nikolai558/FE-BUDDY/releases).

## Verifying a download

Only download FE-BUDDY from this repository's
[Releases](https://github.com/Nikolai558/FE-BUDDY/releases) page. FE-BUDDY's own updater only
downloads from there. Each 3.x release's notes list the SHA-256 of its installer,
`FE-BUDDY-Setup.msi`. To check a downloaded copy, run this in PowerShell and compare the result:

```powershell
Get-FileHash .\FE-BUDDY-Setup.msi -Algorithm SHA256
```

## Disclosure

When a reported vulnerability is confirmed, the FE-BUDDY maintainers decide how to fix and
disclose it based on its nature and severity. Once a fix is available, details may be published
in a GitHub Security Advisory and/or the release notes.

Thank you for helping keep FE-BUDDY and its users secure.
