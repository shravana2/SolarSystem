# Android build

1. Run **Tools > AR Solar System > Build or Repair Project**, then **Validate Project**. Setup enables the ARCore loader for Android XR startup, and validation checks that loader is assigned.
2. Open **File > Build Settings**, select **Android**, and choose **Switch Platform** if needed.
3. Confirm `Assets/Scenes/ARMain.unity` is the enabled build scene.
4. In Player Settings, verify the generated defaults: IL2CPP, ARM64, minimum API 24, OpenGLES3 only, and package identifier `com.arsolarsystem.explorer`. ARCore does not support Vulkan for this configuration. Under **Project Settings > XR Plug-in Management > Android**, confirm ARCore is selected if reviewing the settings manually.
5. Select **Build** and choose an output path such as `Build/ARSolarSystem.apk`.

Unity Android Build Support is installed. SDK/NDK selection should use Unity's embedded Android toolchain for reproducibility. ADB installation can be performed with `adb install -r Build/ARSolarSystem.apk` when an authorized device is connected. No APK or device test has been produced in this checkpoint.
