# AR Solar System Explorer

Unity 2022.3.62f3 Android AR project. It includes a generated ARCore scene, procedural Solar System prefab, selectable planet information, animation, and generated mobile controls for placement, scale, and animation settings.

## Open and configure

Open this folder in Unity 2022.3.62f3 and allow Package Manager to resolve `Packages/manifest.json`. Then run **Tools > AR Solar System > Build or Repair Project**, followed by **Tools > AR Solar System > Validate Project**. The setup command is safe to run again and generates `Assets/Scenes/ARMain.unity`.

The project uses Unity's built-in renderer, AR Foundation 5.0.6, and ARCore XR Plugin 5.0.6. See [SETUP.md](SETUP.md) and [BUILD_ANDROID.md](BUILD_ANDROID.md).

## Current verification state

Phases 1–3 passed Unity Editor validation. Phase 4 adds plane-tap anchor placement/repositioning, separately adjustable body diameter and orbit distance scales, persistent settings, and generated controls. Run **Build or Repair Project** and then **Validate Project** after this update to compile and validate the current scene. A physical ARCore-compatible Android phone is still required to verify camera tracking and AR behavior.
