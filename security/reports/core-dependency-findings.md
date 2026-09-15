# Core dependency findings

Observed on 2026-09-15 with:

```sh
dotnet list api-admisiones/Core/MailORT/MailORT.csproj package --vulnerable --include-transitive --no-restore
dotnet list api-admisiones/Core/LdapService/LdapService.csproj package --vulnerable --include-transitive --no-restore
```

## Findings

| Package | Origin | Resolved | Observed severity | Available remediation | Local impact |
|---|---|---:|---|---:|---|
| `CoreWCF.Primitives` | Direct in `Core/MailORT/MailORT.csproj` | 1.9.0 | Up to critical | 1.9.1 | Advisories include conditional authentication/signature flaws. Applicability depends on the bindings and token validation paths actually enabled. |
| `CoreWCF.NetFramingBase` | Transitive through CoreWCF packages in `Core/MailORT` | 1.9.0 | High | 1.9.1 | The advisory affects reachable net.tcp, net.pipe or Unix-domain framing endpoints. Reachability was not established here. |
| `System.Security.Cryptography.Xml` | Direct in `Core/MailORT` and `Core/LdapService` | 10.0.7 | High | 10.0.12 latest observed; 10.0.10 patches the reviewed July advisories | Advisories affect crafted XML processing. The vulnerable package is included by projects referenced from the API, but an exploitable XML input path was not established. |

The API references both `MailORT` and `LdapService`; this establishes dependency presence, not exploitability. Probable owner: maintainers of the shared `api-admisiones/Core` submodule, coordinated with the API/security owners.

## References and next action

- CoreWCF authentication bypass: <https://github.com/advisories/GHSA-xjr9-gg9q-jx3v>
- CoreWCF framing denial of service: <https://github.com/advisories/GHSA-p86g-xrr2-pf7c>
- .NET XML denial of service: <https://github.com/advisories/GHSA-cvvh-rhrc-wg4q>
- .NET EncryptedXml denial of service: <https://github.com/advisories/GHSA-mmjf-rqrv-855v>

The Core owners should update and test the shared submodule in its own repository, then propagate its pointer. This baseline deliberately does not modify `Core` or create an external ticket.
