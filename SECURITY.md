# Security policy

## Supported versions

Security fixes are provided for the latest released version. Users should update
to the newest available release before reporting an issue that may already have
been fixed.

## Reporting a vulnerability

Do not disclose suspected vulnerabilities in a public issue. Use the repository's
private GitHub security advisory reporting channel. If private reporting is not
available, contact the repository owner without including credentials, connection
strings, production SQL, database exports, or other sensitive data in the first
message.

Include, where possible:

- affected application version and Windows version;
- a minimal reproduction using non-production data;
- expected and observed behavior;
- the potential impact;
- relevant redacted logs.

## Handling sensitive data

Before attaching diagnostics, remove passwords, tokens, connection strings,
database host names, user names, SQL literals containing sensitive values, and
exported business data. Rotate any credential that may have been exposed.

Before publishing or tagging a release, run `pwsh ./scripts/verify-no-tracked-secrets.ps1` from the repository root. See [docs/GITHUB_PUBLISH.md](docs/GITHUB_PUBLISH.md) for the full maintainer checklist.

The project maintainers should acknowledge a valid private report, assess its
severity, prepare a fix and coordinated release, and publish an advisory after
users have had a reasonable opportunity to update.
