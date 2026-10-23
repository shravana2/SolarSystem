using UnityEngine;
using ARSolarSystem.Data;

namespace ARSolarSystem
{
    /// <summary>Deterministic low-resolution fallback maps. These are illustrative procedural textures, not spacecraft imagery.</summary>
    public static class PlanetTextureGenerator
    {
        public static Texture2D Generate(PlanetId id, int width = 256, int height = 128)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, true, false)
            {
                name = id + " Procedural Map",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 2
            };
            var colors = new Color[width * height];
            var seed = (int)id * 17.17f + 3.1f;
            for (var y = 0; y < height; y++)
            {
                var v = (float)y / height;
                for (var x = 0; x < width; x++)
                {
                    var u = (float)x / width;
                    var fine = Mathf.PerlinNoise(u * 18f + seed, v * 9f + seed * 2f);
                    var broad = Mathf.PerlinNoise(u * 5f + seed * 0.3f, v * 3.2f + seed * 0.7f);
                    var band = Mathf.Sin(v * Mathf.PI * 2f * 11f + fine * 2.2f);
                    colors[y * width + x] = Sample(id, u, v, fine, broad, band);
                }
            }
            texture.SetPixels(colors);
            texture.Apply(true, false);
            return texture;
        }

        private static Color Sample(PlanetId id, float longitude, float latitude, float fine, float broad, float band)
        {
            switch (id)
            {
                case PlanetId.Sun:
                    return Color.Lerp(new Color(0.8f, 0.12f, 0.015f), new Color(1f, 0.78f, 0.12f), Mathf.Clamp01(0.28f + fine * 0.58f + band * 0.08f));
                case PlanetId.Mercury:
                    return Color.Lerp(new Color(0.19f, 0.18f, 0.17f), new Color(0.67f, 0.61f, 0.52f), Mathf.Clamp01(broad * 0.7f + fine * 0.4f));
                case PlanetId.Venus:
                    return Color.Lerp(new Color(0.58f, 0.34f, 0.12f), new Color(0.94f, 0.76f, 0.42f), Mathf.Clamp01(0.52f + band * 0.24f + fine * 0.18f));
                case PlanetId.Earth:
                    if (fine > 0.76f && broad > 0.5f) return new Color(0.82f, 0.9f, 0.94f);
                    return broad + fine * 0.18f > 0.62f
                        ? Color.Lerp(new Color(0.08f, 0.28f, 0.12f), new Color(0.64f, 0.61f, 0.34f), fine)
                        : Color.Lerp(new Color(0.015f, 0.08f, 0.31f), new Color(0.08f, 0.43f, 0.78f), broad);
                case PlanetId.Mars:
                    var mars = Color.Lerp(new Color(0.28f, 0.07f, 0.025f), new Color(0.88f, 0.35f, 0.14f), broad * 0.7f + fine * 0.38f);
                    return latitude < 0.045f || latitude > 0.955f ? Color.Lerp(mars, new Color(0.9f, 0.78f, 0.65f), 0.55f) : mars;
                case PlanetId.Jupiter:
                    var jupiter = band > 0f ? new Color(0.78f, 0.61f, 0.4f) : new Color(0.36f, 0.2f, 0.13f);
                    var stormDistance = Mathf.Pow((longitude - 0.72f) * 11f, 2f) + Mathf.Pow((latitude - 0.36f) * 18f, 2f);
                    var storm = Mathf.Exp(-stormDistance);
                    return Color.Lerp(jupiter, new Color(0.72f, 0.19f, 0.08f), storm * 0.85f);
                case PlanetId.Saturn:
                    return Color.Lerp(new Color(0.49f, 0.37f, 0.21f), new Color(0.91f, 0.79f, 0.55f), Mathf.Clamp01(0.55f + band * 0.28f + fine * 0.12f));
                case PlanetId.Uranus:
                    return Color.Lerp(new Color(0.21f, 0.58f, 0.65f), new Color(0.66f, 0.88f, 0.88f), Mathf.Clamp01(0.55f + band * 0.12f + fine * 0.12f));
                default:
                    return Color.Lerp(new Color(0.015f, 0.08f, 0.36f), new Color(0.1f, 0.42f, 0.82f), Mathf.Clamp01(0.55f + band * 0.2f + fine * 0.12f));
            }
        }
    }
}
