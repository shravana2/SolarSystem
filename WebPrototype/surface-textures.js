/* Procedural equirectangular surface maps and lit sphere sprites. */
'use strict';

function createSurfaceMaps(planets) {
  const size = { width: 512, height: 256 };
  const maps = Object.create(null);
  const hash = (x, y, seed) => {
    let n = Math.imul(x ^ seed, 0x45d9f3b) ^ Math.imul(y + seed, 0x119de1f3);
    n = Math.imul(n ^ (n >>> 16), 0x45d9f3b);
    return ((n ^ (n >>> 16)) >>> 0) / 4294967295;
  };
  const smooth = t => t * t * (3 - 2 * t);
  function noise(u, v, frequency, seed) {
    const x = u * frequency, y = v * frequency;
    const ix = Math.floor(x), iy = Math.floor(y), fx = smooth(x - ix), fy = smooth(y - iy);
    const x0 = ((ix % frequency) + frequency) % frequency, x1 = (x0 + 1) % frequency;
    const a = hash(x0, iy, seed), b = hash(x1, iy, seed);
    const c = hash(x0, iy + 1, seed), d = hash(x1, iy + 1, seed);
    return (a + (b - a) * fx) * (1 - fy) + (c + (d - c) * fx) * fy;
  }
  function fbm(u, v, seed, base = 4, octaves = 5) {
    let sum = 0, weight = 0, frequency = base;
    for (let i = 0; i < octaves; i++, frequency *= 2) {
      const w = 1 / (1 << i);
      sum += noise(u, v, frequency, seed + i * 53) * w;
      weight += w;
    }
    return sum / weight;
  }
  const clamp = n => Math.max(0, Math.min(1, n));
  const mix = (a, b, t) => a + (b - a) * clamp(t);
  function rgb(a, b, t) { return [0, 1, 2].map(i => Math.round(mix(a[i], b[i], t))); }

  function makeMap(body, seed) {
    const c = document.createElement('canvas'); c.width = size.width; c.height = size.height;
    const g = c.getContext('2d'), image = g.createImageData(size.width, size.height), data = image.data;
    for (let y = 0; y < size.height; y++) {
      const v = y / size.height, lat = (v - .5) * Math.PI, polar = Math.abs(Math.sin(lat));
      for (let x = 0; x < size.width; x++) {
        const u = x / size.width, broad = fbm(u, v, seed, 4, 5), fine = fbm(u, v, seed + 701, 24, 3);
        let color;
        switch (body) {
          case 'mercury': {
            const basin = fbm(u, v, seed + 9, 5, 3), shade = clamp(.48 + (broad - .5) * .68 + (fine - .5) * .5);
            color = rgb([78, 75, 71], [178, 168, 151], shade);
            if (basin > .73) color = rgb(color, [75, 69, 64], .36);
            break;
          }
          case 'venus': {
            const bands = Math.sin(v * 96 + broad * 8 + fine * 2) * .5 + .5;
            const cloud = clamp(.38 + bands * .34 + (broad - .5) * .26 + (fine - .5) * .2);
            color = rgb([139, 104, 65], [239, 208, 151], cloud);
            break;
          }
          case 'earth': {
            const land = fbm(u, v, seed + 17, 3, 6) + (fine - .5) * .13 - polar * .08;
            const coast = clamp((land - .49) * 22 + .5), ocean = clamp(.34 + broad * .36 - polar * .08);
            color = rgb([5, 28, 69], [18, 105, 150], ocean);
            if (coast > 0) {
              const height = clamp((land - .49) * 2.4), ground = height > .68 ? [172, 157, 128] : [47, 113, 72];
              color = rgb(color, ground, coast);
              if (height > .83) color = rgb(color, [210, 211, 194], (height - .83) * 4);
            }
            const cloud = clamp((fbm(u, v, seed + 91, 13, 4) - .68) * 2.7);
            color = rgb(color, [228, 237, 238], cloud * .48);
            if (polar > .955) color = rgb(color, [225, 233, 235], (polar - .955) * 20);
            break;
          }
          case 'mars': {
            const terrain = clamp(.5 + (broad - .5) * .9 + (fine - .5) * .48), albedo = polar > .94 ? [218, 207, 187] : [0, 0, 0];
            color = rgb([101, 47, 34], [214, 119, 75], terrain);
            if (broad < .38) color = rgb(color, [79, 59, 50], .48);
            if (polar > .94) color = rgb(color, albedo, (polar - .94) * 16);
            break;
          }
          case 'jupiter': {
            const storm = fbm(u, v, seed + 33, 7, 4), band = Math.sin(v * 83 + storm * 5 + fine * 2);
            const tone = clamp(.52 + band * .24 + (broad - .5) * .25);
            color = rgb([133, 91, 65], [237, 210, 174], tone);
            const du = ((u - .73 + 1.5) % 1) - .5, dv = (v - .61) * 1.8;
            const spot = (du * du / .0032 + dv * dv / .00042);
            if (spot < 1) color = rgb(color, [170, 77, 48], .78 + Math.sin(spot * 8) * .08);
            break;
          }
          case 'saturn': {
            const band = Math.sin(v * 104 + broad * 5 + fine * 2), tone = clamp(.6 + band * .16 + (broad - .5) * .18);
            color = rgb([139, 119, 82], [238, 222, 180], tone);
            if (Math.abs(v - .5) < .035) color = rgb(color, [179, 155, 113], .22);
            break;
          }
          case 'uranus': {
            const band = Math.sin(v * 38 + broad * 3), clouds = fbm(u, v, seed + 4, 12, 3);
            color = rgb([55, 139, 157], [177, 225, 221], clamp(.59 + band * .13 + (clouds - .5) * .33));
            break;
          }
          case 'neptune': {
            const band = Math.sin(v * 53 + broad * 5), clouds = fbm(u, v, seed + 14, 11, 4);
            color = rgb([19, 48, 115], [73, 139, 211], clamp(.53 + band * .2 + (clouds - .5) * .32));
            const du = ((u - .67 + 1.5) % 1) - .5, dv = (v - .55) * 1.7;
            if (du * du / .006 + dv * dv / .001 < 1) color = rgb(color, [15, 35, 83], .68);
            break;
          }
          case 'sun': {
            const granule = fbm(u, v, seed + 5, 38, 3), cell = fbm(u, v, seed + 18, 110, 2);
            const heat = clamp(.5 + (granule - .5) * .9 + (cell - .5) * .5);
            color = rgb([171, 39, 11], [255, 229, 113], heat);
            if (granule > .72) color = rgb(color, [255, 247, 179], (granule - .72) * 1.4);
            break;
          }
        }
        const i = (y * size.width + x) * 4;
        data[i] = color[0]; data[i + 1] = color[1]; data[i + 2] = color[2]; data[i + 3] = 255;
      }
    }
    g.putImageData(image, 0, 0);
    if (body === 'mercury') addCraters(g, seed);
    if (body === 'sun') addSunspots(g, seed);
    return c;
  }

  function random(seed) {
    let n = seed >>> 0;
    return () => { n = (Math.imul(n, 1664525) + 1013904223) >>> 0; return n / 4294967296; };
  }
  function addCraters(g, seed) {
    const rand = random(seed + 202), w = size.width, h = size.height;
    for (let i = 0; i < 520; i++) {
      const x = rand() * w, y = rand() * h, r = 1 + Math.pow(rand(), 2) * 15;
      for (const dx of [-w, 0, w]) {
        g.beginPath(); g.arc(x + dx, y, r, 0, Math.PI * 2); g.fillStyle = 'rgba(42,39,37,.28)'; g.fill();
        g.beginPath(); g.arc(x + dx - r * .13, y - r * .13, r * .78, 0, Math.PI * 2); g.strokeStyle = 'rgba(223,211,190,.42)'; g.lineWidth = Math.max(.6, r * .08); g.stroke();
      }
    }
  }
  function addSunspots(g, seed) {
    const rand = random(seed + 88);
    for (let i = 0; i < 20; i++) {
      const x = rand() * size.width, y = 25 + rand() * (size.height - 50), r = 2 + rand() * 9;
      g.beginPath(); g.ellipse(x, y, r * 1.5, r, rand() * 2, 0, Math.PI * 2); g.fillStyle = 'rgba(98,30,10,.48)'; g.fill();
      g.beginPath(); g.ellipse(x, y, r * 1.18, r * .7, 0, 0, Math.PI * 2); g.fillStyle = 'rgba(50,24,14,.7)'; g.fill();
    }
  }
  const seeds = {mercury:41,venus:73,earth:107,mars:139,jupiter:173,saturn:211,uranus:251,neptune:293,sun:337};
  planets.forEach(p => { maps[p.id] = makeMap(p.id, seeds[p.id]); });
  maps.sun = makeMap('sun', seeds.sun);
  return maps;
}

function loadSolarTextureMaps(maps) {
  const files = {mercury:'mercury',venus:'venus',earth:'earth',mars:'mars',jupiter:'jupiter',saturn:'saturn',uranus:'uranus',neptune:'neptune',sun:'sun'};
  for (const [body, file] of Object.entries(files)) {
    const image = new Image();
    image.onload = () => {
      const map = document.createElement('canvas');
      map.width = 1024; map.height = 512;
      map.getContext('2d').drawImage(image, 0, 0, map.width, map.height);
      maps[body] = map;
    };
    image.src = `assets/textures/${file}.jpg`;
  }
}

function createSphereRenderer(maps) {
  const cache = Object.create(null);
  const sourcePixels = new WeakMap();
  return function drawTexturedSphere(body, x, y, radius, rotation, tilt = 0, isSun = false, orientation = null) {
    if (radius < .2) return false;
    const map = maps[body.id];
    if (!map) return false;
    const diameter = 256;
    const phase = Math.round(((rotation / (Math.PI * 2)) % 1 + 1) % 1 * 96) % 96;
    const tiltPhase = Math.round((tilt / (Math.PI * 2) + 1) % 1 * 96) % 96;
    const orientationKey = orientation ? [orientation.right, orientation.up, orientation.front].flatMap(v => [v.x,v.y,v.z]).map(v => v.toFixed(2)).join(',') : '';
    const key = body.id;
    let entry = cache[key];
    if (!entry) {
      const canvas = document.createElement('canvas'); canvas.width = canvas.height = diameter;
      entry = cache[key] = {canvas, phase: -1};
    }
    if (entry.phase !== phase || entry.tiltPhase !== tiltPhase || entry.map !== map || entry.orientationKey !== orientationKey) {
      const ctx = entry.canvas.getContext('2d'), out = ctx.createImageData(diameter, diameter), pixels = out.data;
      let source = sourcePixels.get(map);
      if (!source) { source = map.getContext('2d').getImageData(0, 0, map.width, map.height).data; sourcePixels.set(map, source); }
      const angle = phase / 96 * Math.PI * 2, pitch = tiltPhase / 96 * Math.PI * 2;
      const cosA = Math.cos(angle), sinA = Math.sin(angle), cosP = Math.cos(pitch), sinP = Math.sin(pitch);
      for (let py = 0; py < diameter; py++) for (let px = 0; px < diameter; px++) {
        const nx = (px + .5) / diameter * 2 - 1, ny = (py + .5) / diameter * 2 - 1, rr = nx * nx + ny * ny;
        const at = (py * diameter + px) * 4;
        if (rr > 1) continue;
        const nz = Math.sqrt(1 - rr);
        let turnedX, turnedY, localZ;
        if (orientation) {
          // Canvas y grows downward, while the camera basis uses positive y up.
          const cameraY = -ny;
          turnedX = nx * orientation.right.x + cameraY * orientation.right.y + nz * orientation.right.z;
          turnedY = nx * orientation.up.x + cameraY * orientation.up.y + nz * orientation.up.z;
          localZ = nx * orientation.front.x + cameraY * orientation.front.y + nz * orientation.front.z;
        } else {
          turnedX = nx * cosA + nz * sinA;
          const turnedZ = -nx * sinA + nz * cosA;
          turnedY = ny * cosP - turnedZ * sinP;
          localZ = ny * sinP + turnedZ * cosP;
        }
        const u = ((Math.atan2(turnedX, localZ) / (Math.PI * 2) + .5) % 1 + 1) % 1;
        const v = .5 - Math.asin(Math.max(-1, Math.min(1, turnedY))) / Math.PI;
        const sx = Math.floor(u * map.width) % map.width, sy = Math.min(map.height - 1, Math.floor(v * map.height));
        const si = (sy * map.width + sx) * 4;
        const light = Math.max(0, nx * -.42 + ny * -.48 + nz * .77);
        const illumination = isSun ? .76 + light * .24 : .13 + light * .94;
        pixels[at] = Math.min(255, source[si] * illumination);
        pixels[at + 1] = Math.min(255, source[si + 1] * illumination);
        pixels[at + 2] = Math.min(255, source[si + 2] * illumination);
        pixels[at + 3] = 255;
      }
      ctx.putImageData(out, 0, 0); entry.phase = phase; entry.tiltPhase = tiltPhase; entry.map = map; entry.orientationKey = orientationKey;
    }
    return entry.canvas;
  };
}
