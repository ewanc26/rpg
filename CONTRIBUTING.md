# Contributing to the RPG template

A minimal top-down RPG starter for Godot **4.7** with the **.NET (C#)** build — a from-scratch scaffold for the Godot-Mono + RPG combination, since nothing else covered that gap. Thanks for wanting to help!

## Getting started

```bash
./bootstrap.sh          # installs Godot .NET + .NET SDK if needed, imports the project
./run-smoke-test.sh     # verifies everything works — 65 checks
```

`bootstrap.sh` handles macOS, SteamOS and generic Linux, is safe to re-run, and `--check` shows what's missing without installing.

## Notes for this template

- **It must stay a template.** Environment-specific or content-heavy additions fight the point: the repo's job is to be a clean, minimal scaffold someone else starts a real game from.
- Gameplay input goes through project-defined actions (`move_left`, …, `interact`) rather than built-in `ui_*` actions, keeping menu navigation independent — preserve that in any input change.
- The smoke suite (`./run-smoke-test.sh`, `scripts/Tests/SmokeTest.cs`) is the gate: it checks input map, movement, collisions, pickups, the dialogue signal chain, stats, inventory and save/load headlessly. New features should add checks; the suite exits non-zero on any failure, and it doubles as the CI gate.
- "Minimal" is a feature: before adding a system, ask whether an average user would delete it after forking.

## Before opening a PR

- `./run-smoke-test.sh` passes locally (all 65+ checks).
- Conventional commits; focused PRs; note what the smoke coverage now includes.
- AI-assisted PRs welcome; stand behind every line and run the suite yourself.

## Licence

See [LICENCE](LICENCE)/[LICENSE](LICENSE). Contributions accepted under the same terms.