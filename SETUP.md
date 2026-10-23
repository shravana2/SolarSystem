# Setup

## Prerequisites

- Unity Editor 2022.3.62f3 with Android Build Support, Android SDK & NDK Tools, and OpenJDK modules.
- Internet access for Unity Package Manager on the first import.
- ARCore-compatible Android device for runtime validation.

The project pins AR Foundation and ARCore XR Plugin to 5.0.6, the released package versions for Unity 2022.3. This uses the built-in render pipeline. Unity resolves transitive XR dependencies from the package manifest.

## Automated scene setup

1. Open the project in Unity and let package resolution finish.
2. Run **Tools > AR Solar System > Build or Repair Project**. This generates the AR scene, detected-plane prefab, procedural planet textures/materials, `Assets/Prefabs/SolarSystem.prefab`, and mobile controls.
3. Open `Assets/Scenes/ARMain.unity` and use **Tools > AR Solar System > Validate Project**. This checks AR references, Solar System data/assets, scale behavior, and motion behavior.

The setup command also assigns ARCore as the Android XR loader and enables XR startup. Validation reports an error if that loader configuration is missing.

The generated scene contains an AR Session, XR Origin, AR camera with camera background, horizontal plane manager with translucent plane mesh, raycast manager, anchor manager, UI EventSystem, planet information panel, and mobile controls. Setup imports TextMeshPro Essential Resources automatically if they are missing. Use **Place Solar System**, scan a horizontal surface, and tap it to anchor the model. **Reset Placement** clears the model; use Place again to reposition.

Enter a Sun diameter in meters and press **Apply Size** to resize the Sun and planets proportionally. The distance slider independently changes orbit spacing in meters per AU. Defaults are 0.10 m for the Sun and 0.25 m/AU. These separate scales are a usability choice and do not represent a complete scale model. Scale and animation mode preferences persist between launches.

## Environment inventory at project creation

Unity 2022.3.62f3 is installed with Android Build Support, SDK/NDK tools, and bundled OpenJDK. A system Java 21, Android SDK platforms 33 and 36, and NDK 23.1, 25.1, and 29.0 are also present. ADB is installed but its daemon could not start in the restricted environment. No Android device was reported.

## Troubleshooting

- If packages remain unresolved, check Unity Package Manager network access and re-open the project.
- If Editor compilation fails, inspect the first C# compiler error in the Console before building.
- If ARCore is unavailable, verify device certification and Google Play Services for AR on the phone.
