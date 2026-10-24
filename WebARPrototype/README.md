# Orbit AR Solar System prototype

Standalone mobile-first WebGL and WebXR prototype. It uses Three.js for a 3D preview and WebXR `immersive-ar` with hit testing for surface placement. The existing `WebPrototype/` remains the original desktop Canvas prototype.

## Run

Three.js is imported from jsDelivr, so a network connection is needed to load the renderer. From the repository root:

```sh
python3 -m http.server 8000
```

Open `http://localhost:8000/WebARPrototype/` for the 3D fallback. WebXR AR requires a supported mobile browser/device and a secure origin (HTTPS; localhost is treated as secure for local development). To test on a phone, serve this directory over HTTPS and open its URL on the device.

## Included

- 3D Sun, planets, inclined elliptical orbit paths, local surface textures, and orbital/axial animation based on the prototype's JPL approximate Keplerian elements.
- Runtime AR capability check and a desktop/non-AR 3D fallback.
- WebXR hit-test reticle and tap-to-place flow for horizontal surfaces.
- Independent Sun diameter and orbital-distance controls, simulation speed, reset placement, pause/resume, and planet information cards.
- Touch selection in AR and pointer selection in the 3D preview.

## Scale and known limits

The Sun diameter and orbital spacing use separate scales for practical placement. Planet diameters follow the same ratio to the Sun with a 3 mm minimum visual diameter so small planets remain visible/selectable. Orbit paths and starting positions use approximate Keplerian elements from the existing browser prototype; values are approximate and the element model is intended for dates from 1800 to 2050. WebXR availability depends on the browser, operating system, device AR services, permissions, and HTTPS; the app checks support at runtime and keeps the preview available otherwise. See the [Three.js WebXR manager documentation](https://threejs.org/docs/pages/WebXRManager.html) and [MDN WebXR security notes](https://developer.mozilla.org/en-US/docs/Web/API/WebXR_Device_API/Permissions_and_security).

Textures are bundled locally. Attribution and licensing details are in [ASSET_CREDITS.md](ASSET_CREDITS.md).
