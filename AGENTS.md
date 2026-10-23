# Repository Guidelines

## Project Structure & Module Organization

This repository contains a Unity 2022.3.62f3 Android AR project and a standalone browser prototype. Runtime C# code lives under `Assets/Scripts/Runtime`, UI code under `Assets/Scripts/UI`, planetary data under `Assets/Scripts/Data`, and editor setup/validation tools under `Assets/Editor`. Unity scenes, prefabs, materials, and procedural textures are under `Assets/Scenes`, `Assets/Prefabs`, `Assets/Materials`, and `Assets/Textures`. Keep Unity `.meta` files alongside assets; Unity uses their GUIDs to preserve references. `WebPrototype/` is independent of Unity's `Assets` folder. Project setup and architecture are described in `SETUP.md` and `ARCHITECTURE.md`.

## Build, Test, and Development Commands

Open the repository in Unity 2022.3.62f3 and let Package Manager resolve `Packages/manifest.json`. Run **Tools > AR Solar System > Build or Repair Project**, then **Tools > AR Solar System > Validate Project** to generate and check the scene and assets. For Android, select Android in **File > Build Settings**, confirm `Assets/Scenes/ARMain.unity` is enabled, and use **Build**; see `BUILD_ANDROID.md`. Test AR tracking and placement on an ARCore-compatible Android device. Run the browser prototype with `cd WebPrototype && python3 -m http.server 8000`, then open `http://localhost:8000`.

## Coding Style & Naming Conventions

Follow existing C# conventions: four-space indentation, PascalCase for types and public members, and descriptive camelCase for local variables and parameters. Keep planetary facts centralized in `PlanetaryBodyData`; keep scene generation in editor setup code and runtime behavior in the corresponding runtime/UI scripts. Prefer small focused changes and preserve serialized Unity references. Use descriptive filenames matching C# class names.

## Testing Guidelines

There is no separate repository test suite. Use **Validate Project** after setup or code changes; it checks generated content, references, motion, and scale behavior. Follow `TESTING.md` for Play mode interaction checks and the Android device checklist. Verify browser prototype changes by serving `WebPrototype/` locally and exercising its controls in a modern browser.

## Commit & Pull Request Guidelines

Git history is unavailable in this checkout, so no established commit format can be confirmed. Use short imperative commit subjects (for example, `Fix AR placement reset`). Pull requests should summarize behavior changes, list validation performed and device/browser coverage, link related issues when applicable, and include screenshots or a short recording for visible UI or scene changes.

## Asset and Configuration Notes

Do not commit generated build output, Unity `Library` or `Temp` contents, or local machine settings. Retain `.meta` files for tracked Unity assets. Keep package changes explicit in `Packages/manifest.json` and document Android or XR configuration changes in the relevant setup guide.
