# Test Tycoon

Unity project for the fruit farm simulation described in [Requirement.txt](Requirement.txt).

## Open the project

Open this repository folder with Unity **6000.3.16f1**. Unity will restore packages from `Packages/manifest.json`; the required project assets and settings are in `Assets/` and `ProjectSettings/`.

## Architecture discussion

The architecture is being reviewed in separate documents. Start at [docs/architecture/README.md](docs/architecture/README.md). The selected dependency approach is manual dependency injection wired in a scene Bootstrap Root, with asmdef boundaries; gameplay implementation has not begun yet.

`demo-interview-4.5.apk` is the supplied reference demo in the local workspace. APK files and Unity-generated folders are excluded from Git by `.gitignore`.
