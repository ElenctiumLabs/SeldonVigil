# SeldonVigil CLI

Command-line client for the SeldonVigil governance platform: register and manage
agents, manage security policies, query and export traces, manage cost budgets,
and authenticate against a SeldonVigil API.

## Installation

```bash
dotnet tool install --global SeldonVigil.Cli
```

From a repository checkout, without installing the tool:

```bash
dotnet run --project src/SeldonVigil.Cli -- agent list
```

## Authentication

```bash
seldonvigil auth login --api-key <api-key> --url http://localhost:8080
seldonvigil auth whoami
seldonvigil auth logout
```

## Commands

- `agent` - list, get, create, activate, suspend and delete registered agents
- `policy` - list, get, create, disable and evaluate security policies
- `trace` - list, get and export observability traces
- `budget` - list, create and reset cost budgets
- `seed` - seed demo agents and traces for development and testing

Run `seldonvigil <command> --help` for the options each command accepts.

## Examples

```bash
seldonvigil agent list
seldonvigil agent create --name "Seldon Lattice" --framework seldon-lattice
seldonvigil policy evaluate --input "ignore all previous instructions"
seldonvigil trace export --format json --output traces.json
```
