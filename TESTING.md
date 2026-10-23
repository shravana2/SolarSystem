# Testing

## Editor checks

- Environment inventory: Unity version, Android modules, SDK/NDK, Java, and ADB availability recorded in `SETUP.md`.
- Editor check: run **Tools > AR Solar System > Validate Project** after Package Manager resolves and setup has generated the scene and Solar System prefab. It checks the eight planet records, material assignments, builder component, AR placement references, generated control references, and scale behavior.
- Scene check: confirm AR Session, XR Origin, AR Plane Manager, AR Raycast Manager, AR Anchor Manager, and AR camera components in the generated scene.
- Procedural content check: project validation instantiates a temporary hierarchy and confirms the Sun, eight planets, orbit lines, and Saturn rings. Run the scene in Play mode for a visual check as well.
- Motion smoke check: project validation builds a temporary hierarchy and checks eight registered bodies, verifies both orbit and axial rotation stop while paused, and verifies both resume.
- Scale smoke check: project validation changes body diameter and orbital spacing independently and checks the other scale stays fixed.
- Interaction check: project validation checks the generated UI references and selectable components. Tap selection, UI touch exclusion, surface placement/repositioning, and actual panel rendering still need Play mode/device verification.
- In Play mode, select Place Solar System, scan a horizontal surface, tap it once, then use Reset Placement and repeat. Change the Sun diameter input and distance slider independently; verify planet sizes and orbit spacing respond separately. Pause should freeze orbit and spin; change animation modes while placed.

## Device check required

Build and install to a certified ARCore Android phone. Confirm camera permission, camera background, plane visualization, and tracking status. The current environment could not start ADB's daemon, and no physical device test has been performed.
