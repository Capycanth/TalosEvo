using Microsoft.Xna.Framework;
using System.Collections.Generic;
using TalosEvo.Core.enumeration;

namespace TalosEvo.EvoUtil
{
    public static class EvoConstants
    {
        public static class BiomeLookup
        {
            // TOD0: Create better Biome Dictionary
            public static readonly Dictionary<(float tempMin, float tempMax, float rainMin, float rainMax), Biome> Biomes =
                new Dictionary<(float tempMin, float tempMax, float rainMin, float rainMax), Biome>
                {
                    { (0.8f, 1f, 0f, 0.3f), Biome.Desert },
                    { (0.5f, 0.8f, 0.5f, 1f), Biome.Forest },
                    { (0.5f, 0.8f, 0.3f, 0.5f), Biome.Grassland },
                    { (0f, 0.3f, 0f, 0.5f), Biome.Tundra },
                    { (0.3f, 0.5f, 0.3f, 0.5f), Biome.Taiga },
                    { (0.8f, 1f, 0.5f, 1f), Biome.Jungle },
                    { (0.7f, 0.9f, 0.3f, 0.5f), Biome.Savanna },
                    { (0.5f, 0.7f, 0.7f, 1f), Biome.Wetlands },
                    { (0.8f, 1f, 0.7f, 1f), Biome.Alpine },
                };

            public static Biome GetBiome(float temperature, float rainfall, float elevation, bool isRiver, bool isLake)
            {
                if (isRiver) return Biome.River;
                if (isLake) return Biome.Lake;
                if (elevation > 0.90f) return Biome.Mountain;

                foreach (var kvp in Biomes)
                {
                    var (tempMin, tempMax, rainMin, rainMax) = kvp.Key;
                    if (temperature >= tempMin && temperature <= tempMax &&
                        rainfall >= rainMin && rainfall <= rainMax)
                    {
                        return kvp.Value;
                    }
                }
                return Biome.Desert; // Default biome if none matches
            }

            public static Color GetBiomeColor(Biome biomeType)
            {
                return biomeType switch
                {
                    Biome.Desert => Color.SandyBrown,
                    Biome.Forest => Color.ForestGreen,
                    Biome.Grassland => Color.LightGreen,
                    Biome.Tundra => Color.LightGray,
                    Biome.Mountain => Color.Gray,
                    Biome.River => Color.Blue,
                    Biome.Lake => Color.DarkBlue,
                    Biome.Jungle => Color.DarkGreen,
                    Biome.Savanna => Color.Goldenrod,
                    Biome.Taiga => Color.OliveDrab,
                    Biome.Wetlands => Color.SeaGreen,
                    Biome.Alpine => Color.White,
                    _ => Color.Black,
                };
            }
        }
    }
}
