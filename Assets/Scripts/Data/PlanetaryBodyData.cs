using System.Collections.Generic;

namespace ARSolarSystem.Data
{
    public enum PlanetId
    {
        Sun, Mercury, Venus, Earth, Mars, Jupiter, Saturn, Uranus, Neptune
    }

    /// <summary>Static reference values based on NASA GSFC NSSDCA's Planetary Fact Sheet (metric).</summary>
    public sealed class PlanetaryBodyData
    {
        public PlanetId Id { get; }
        public string DisplayName { get; }
        public double DiameterKm { get; }
        public double MassKg { get; }
        public double MeanDistanceFromSunKm { get; }
        public double OrbitalPeriodDays { get; }
        /// <summary>Sidereal rotation in hours; negative values indicate retrograde rotation.</summary>
        public double SiderealRotationHours { get; }
        public int? ConfirmedMoonCount { get; }
        public string Description { get; }

        public PlanetaryBodyData(PlanetId id, string name, double diameterKm, double massKg,
            double meanDistanceFromSunKm, double orbitalPeriodDays, double rotationHours,
            int? moons, string description)
        {
            Id = id;
            DisplayName = name;
            DiameterKm = diameterKm;
            MassKg = massKg;
            MeanDistanceFromSunKm = meanDistanceFromSunKm;
            OrbitalPeriodDays = orbitalPeriodDays;
            SiderealRotationHours = rotationHours;
            ConfirmedMoonCount = moons;
            Description = description;
        }
    }

    public static class PlanetaryData
    {
        public const double SunDiameterKm = 1_392_700.0;
        private static readonly PlanetaryBodyData[] Bodies =
        {
            new PlanetaryBodyData(PlanetId.Mercury, "Mercury", 4879, 3.30e23, 57_900_000, 88.0, 1407.6, 0,
                "The smallest planet has a heavily cratered surface and a year that lasts just 88 Earth days."),
            new PlanetaryBodyData(PlanetId.Venus, "Venus", 12104, 4.87e24, 108_200_000, 224.7, -5832.5, 0,
                "A dense carbon dioxide atmosphere traps heat, making Venus the hottest planet."),
            new PlanetaryBodyData(PlanetId.Earth, "Earth", 12756, 5.97e24, 149_600_000, 365.2, 23.9, 1,
                "Earth has stable surface liquid water and is the only world currently known to support life."),
            new PlanetaryBodyData(PlanetId.Mars, "Mars", 6792, 6.42e23, 228_000_000, 687.0, 24.6, 2,
                "Mars is a cold desert world with ancient river valleys, polar ice, and the tallest volcano known."),
            new PlanetaryBodyData(PlanetId.Jupiter, "Jupiter", 142984, 1.898e27, 778_500_000, 4331, 9.9, 95,
                "The largest planet is a hydrogen-rich gas giant with powerful storms, including the Great Red Spot."),
            new PlanetaryBodyData(PlanetId.Saturn, "Saturn", 120536, 5.68e26, 1_432_000_000, 10747, 10.7, 274,
                "Saturn is a gas giant whose broad, bright rings are made mostly of water-ice particles."),
            new PlanetaryBodyData(PlanetId.Uranus, "Uranus", 51118, 8.68e25, 2_867_000_000, 30589, -17.2, 28,
                "Uranus is an ice giant that rotates on a strongly tilted axis, giving it extreme seasons."),
            new PlanetaryBodyData(PlanetId.Neptune, "Neptune", 49528, 1.02e26, 4_515_000_000, 59800, 16.1, 16,
                "Neptune is a distant ice giant with supersonic winds and a large moon, Triton."),
        };

        public static IReadOnlyList<PlanetaryBodyData> Planets => Bodies;

        public static PlanetaryBodyData Get(PlanetId id)
        {
            for (var i = 0; i < Bodies.Length; i++)
                if (Bodies[i].Id == id) return Bodies[i];
            return null;
        }
    }
}
