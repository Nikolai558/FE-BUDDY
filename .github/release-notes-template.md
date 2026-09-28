<!--
  FE-BUDDY's GitHub release notes. .github/scripts/Build-ReleaseAssets.ps1 fills this in for every
  release: {{TOKENS}} are replaced, "IF X" ... "END IF X" blocks are kept only when X applies, and
  every HTML comment (like this one) is removed. {{CHANGELOG}} is the version's section of
  ChangeLog.md. FE-BUDDY's update window shows the result, so keep to simple Markdown.
  See docs/Developers/RELEASING.md.
-->
<!-- IF PRERELEASE -->
> **Pre-release ({{CHANNEL}}):** for testing, and it may have bugs. FE-BUDDY's updater only offers it if your update channel in Settings includes {{CHANNEL}} releases.

<!-- END IF PRERELEASE -->
## Instructions to install:
- **Already have FE-BUDDY 2.9 or newer:** Launch FE-BUDDY and accept the update prompt. It downloads and runs the new installer for you.
- **New install, or auto-update isn't working:** [Download {{MSI_NAME}}]({{MSI_URL}}), run it, and follow the prompts.


## Change log:
{{CHANGELOG}}

---
<!-- IF PREVIOUS -->
[Every change since {{PREVIOUS_VERSION}}]({{COMPARE_URL}}) ·
<!-- END IF PREVIOUS -->
`{{MSI_NAME}}` SHA-256: `{{SHA256}}`
