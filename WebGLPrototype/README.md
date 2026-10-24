# Solar System Explorer — WebGL

A standalone browser project built with the bundled Three.js 0.160.0 classic build. The rewrite keeps the existing controls and view menu while using one scene setup and one pointer/zoom path for the available views.

For the mobile WebXR surface placement prototype, see [`../WebARPrototype/README.md`](../WebARPrototype/README.md). It is a separate mobile-first page so this desktop explorer and its classic Three.js build remain unchanged.

## Run locally

From `WebGLPrototype/`, start a local server:

```sh
python3 -m http.server 8000
```

Open <http://localhost:8000> in a WebGL capable browser. The project has no CDN or package installation requirement. The Three.js MIT license is in `vendor/THREE-LICENSE.txt`.

## Publish with GitHub Pages

The repository workflow at `.github/workflows/deploy-webgl.yml` publishes both browser prototypes whenever changes are pushed to `main` (or when run manually from the Actions tab). In the GitHub repository, set **Settings → Pages → Build and deployment → Source** to **GitHub Actions**. After the first successful workflow run, this site is available at `https://<owner>.github.io/<repository>/` and the WebAR prototype at `https://<owner>.github.io/<repository>/WebARPrototype/`.

## Controls and features

- Drag to orbit the model. Shift + drag or right-drag to pan. Scroll or use the `+` and `−` buttons to zoom. With a body selected, scrolling smoothly centers and zooms toward it; zooming back out brings the rest of the system back into view.
- **Views** offers Top view, Orbital lateral view, and Size comparison. Size comparison hides the star field, centers the Sun above a compact planet row, and preserves diameter ratios. Select a body, then scroll to center and zoom toward it; zooming out restores the full comparison, and clicking empty space resets it.
- Orbital views show clickable planet arrows with names and direction of motion, plus a toggleable reference grid with one astronomical unit (AU) per cell. The AU labels mark distances from the Sun; the To-scale view leaves orbital distances out of the comparison.
- Select the Sun or a planet to open its profile. Pause with a selected body to frame it; drag in orbit view to inspect its surface.
- Set the simulation speed, pause motion, toggle planet pointers, adjust the Sun scale, reset settings, or reset the model position.
- Help and About describe the model and texture credits.

The orbital views use one linear scale for planet sizes and orbital distances. One astronomical unit is converted from the Sun's displayed diameter; ellipses use approximate J2000 semi-major axes, eccentricities, inclinations, ascending nodes, and perihelia. Size comparison keeps the controls and selected-body profile available beside the comparison. Each planet's listed period drives its motion. Planet and Sun textures are bundled under `assets/textures/`; see [ASSET_CREDITS.md](ASSET_CREDITS.md) for attribution and license details.
