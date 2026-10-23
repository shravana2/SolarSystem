# Orbit — Solar System Explorer prototype

This is a standalone, mouse-driven web prototype for settling the Solar System's visual layout and interactions before rebuilding them in AR. It sits outside Unity's `Assets` folder, has no package manager dependencies, and does not modify the Unity project.

## Run locally

From this directory, run:

```sh
python3 -m http.server 8000
```

Open <http://localhost:8000> in Firefox, Chrome, or another modern browser. Stop the local server with `Ctrl+C`.

## Interactions

- Use the **Views** menu for Top View, Orbital Lateral View, or To-scale Comparison. The comparison preserves Sun and planet diameters while arranging them in a compact layout; orbital spacing is omitted in that view. Use the mouse wheel or `+`/`−` controls to zoom; zooming keeps the displayed diameter ratios intact. When a body is selected, wheel zoom centers on and follows it in every view. Otherwise, wheel zoom anchors to the pointer in orbital views. Drag on the model to orbit the view; use `Shift` + drag (or right-drag) to pan.
- Click the Sun or a planet to open its information card. Click another body to switch selection.
- Set the Sun diameter to scale all body diameters and mean orbital distances together.
- Pause/resume, select a simulation speed, reset the view/model position, or restore defaults.
- Pause the simulation and select a planet or the Sun to focus it; drag its disk to inspect the surface from different angles.
- Toggle moving orbit pointers. With animation paused, select a planet to focus it; use **Zoom to body** in its information card to focus at any time.
- The help button and “About this model” explain the scale compromises.

Planet diameters and mean orbital distances are rendered from the same Sun-diameter ratio. The model coordinate is expressed in Sun diameters: a planet's diameter is `planet diameter / Sun diameter`, and its orbit radius is `mean distance / Sun diameter`. The Sun diameter control therefore scales every body and orbit together. No separate distance-scale control or planet size floor is used.

At 1×, simulation time advances at real-time seconds. Select 10×, 100×, 1,000×, 10,000×, 100,000×, or 1,000,000× to speed up the same clock for orbital motion and axial rotation. Planetary periods remain based on their stated real-world values, so the period ratios are preserved.

This has an unavoidable display consequence: at a single shared scale, planets are tiny compared with their orbital distances, and the outer planets cannot fit alongside the inner planets. Use zoom and pan to explore different regions. With no focused body, wheel zoom anchors the point under the pointer on the orbital plane instead of zooming toward the Sun. The camera uses perspective projection and can track a selected planet; focusing automatically brings that body's disk to about 18% of the view height while leaving all other dimensions and distances unchanged. Canvas hit areas are larger than the rendered planet disks so subpixel bodies can still be selected. Pointer arrows are screen-space location markers and do not change body sizes. Planet and Sun textures are bundled locally. See [ASSET_CREDITS.md](ASSET_CREDITS.md) for source and license details.

## Texture assets

The prototype uses locally bundled Solar System Scope equirectangular maps for the Sun and all eight planets. They are downsampled to 1024 × 512 when loaded. See [ASSET_CREDITS.md](ASSET_CREDITS.md) for attribution and license details.

## Data and future work

Startup positions use the browser’s current date and JPL approximate planetary Keplerian elements (J2000 ecliptic frame; approximation valid 1800–2050). The renderer solves Kepler’s equation for each planet, draws the resulting inclined ellipses, and advances each mean anomaly at its own orbital period. Thus orbital speeds vary along each ellipse and preserve their ratios to Earth.

Planet spin-axis directions use the NASA/JPL NAIF SPICE PCK J2000 pole right ascension and declination values, transformed into the model ecliptic frame and rotated with the camera. Saturn’s ring plane follows its pole. Planet values and short descriptions are carried over from the Unity project's central `PlanetaryBodyData` records. Values are approximate/reference values; rotation is sidereal and negative rotation values indicate retrograde motion. Moon counts can change as new moons are confirmed. See [`ASSET_SOURCES.md`](../ASSET_SOURCES.md) for the NASA GSFC NSSDCA Planetary Fact Sheet reference and source date noted by the project.

This is a first interaction prototype. Once its controls and behavior are approved through browser use, the renderer and input layer can be replaced with a 3D scene and then adapted to AR placement while retaining the data and interaction decisions.
