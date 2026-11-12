# SeldonVigil.Cli

A .NET global tool built with `System.CommandLine`. Depends on `SeldonVigil.Sdk` for all API calls.

## Command surface

| Command | Sub-commands |
|---|---|
| `seldonvigil auth` | `login`, `whoami`, `logout` |
| `seldonvigil agent` | `list`, `get`, `create`, `suspend`, `activate`, `delete` |
| `seldonvigil policy` | `list`, `get`, `create`, `disable`, `evaluate` |
| `seldonvigil trace` | `list`, `get`, `export` |
| `seldonvigil budget` | `list`, `create`, `reset` |
| `seldonvigil seed` | Seeds demo data |

Output formats: table (default), JSON (`--output json`), YAML (`--output yaml`).
