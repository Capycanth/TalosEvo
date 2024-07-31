using Microsoft.Xna.Framework;
using System;
using TalosEvo.Core.enumeration;

namespace TalosEvo.EvoUtil
{
    public static class EvoConstants
    {
        public static class BiomeLookup
        {
            // TOD0: Create better Biome Dictionary
            public static readonly byte[,] Biomes = new byte[20,20]
            {
                { 0,0,0,0,0,0,0,0,0,0,9,9,9,9,9,9,9,9,9,9 },
                { 0,0,0,0,0,0,0,0,0,0,9,9,9,9,9,9,9,9,9,9 },
                { 0,0,0,0,0,0,0,0,0,0,0,9,9,9,9,9,9,9,9,9 },
                { 0,0,0,0,0,0,0,0,0,0,0,9,9,9,9,9,9,9,9,9 },
                { 0,0,0,4,4,4,4,4,4,4,4,9,9,9,9,9,9,9,9,9 },
                { 0,0,0,4,4,4,4,4,4,4,4,9,9,9,9,9,9,9,9,9 },
                { 1,2,2,4,4,4,4,4,4,4,4,7,7,7,7,7,7,7,8,8 },
                { 1,2,2,2,2,2,2,2,2,2,7,7,7,7,7,7,7,8,8,8 },
                { 1,1,2,2,2,2,2,2,2,2,7,7,7,7,7,7,7,8,8,8 },
                { 1,1,2,2,2,2,2,2,2,2,7,7,7,7,7,7,7,8,8,8 },
                { 1,1,1,1,2,2,2,2,2,2,2,2,7,7,7,7,7,8,8,8 },
                { 1,1,1,1,2,2,2,2,2,2,2,2,7,7,7,7,7,8,8,8 },
                { 1,1,1,1,2,2,2,2,2,2,2,2,2,7,7,7,7,8,8,8 },
                { 1,1,1,1,2,2,2,2,2,2,2,2,2,7,7,7,7,8,8,8 },
                { 1,1,1,1,2,2,2,2,2,2,2,2,2,6,6,6,6,6,8,8 },
                { 1,1,1,1,1,5,5,5,5,5,5,5,5,6,6,6,6,6,6,8 },
                { 1,1,1,1,1,5,5,5,5,5,5,5,5,6,6,6,6,6,6,6 },
                { 1,1,1,1,1,5,5,5,5,5,5,5,5,6,6,6,6,6,6,6 },
                { 1,1,1,1,1,1,5,5,5,5,5,5,6,6,6,6,6,6,6,6 },
                { 1,1,1,1,1,1,5,5,5,5,5,6,6,6,6,6,6,6,6,6 },
            };

            public static Biome GetBiome(float temperature, float rainfall, float elevation, bool isRiver, bool isLake)
            {
                if (isLake) return Biome.Lake;
                if (isRiver) return Biome.River;
                if (elevation > 0.85f) return Biome.Mountain;

                int adjustedRain = (int)Math.Floor(rainfall * 20);
                if (adjustedRain == 20) adjustedRain = 19;
                int adjustedTemp = (int)Math.Floor(temperature * 20);
                if (adjustedTemp == 20) adjustedTemp = 19;

                return (Biome)Biomes[adjustedRain, adjustedTemp];
            }

            public static Color GetBiomeColor(Biome biomeType)
            {
                return biomeType switch
                {
                    Biome.Desert => Color.SandyBrown,
                    Biome.Grassland => Color.LightGreen,
                    Biome.Tundra => Color.LightGray,
                    Biome.Mountain => Color.Brown,
                    Biome.Lake => Color.DarkBlue,
                    Biome.Jungle => Color.DarkGreen,
                    Biome.Savannah => Color.PaleGreen,
                    Biome.Swamp => Color.DarkSeaGreen,
                    Biome.ConiferousForest => Color.LightSeaGreen,
                    Biome.Arctic => Color.GhostWhite,
                    Biome.TemperateForest => Color.Green,
                    Biome.River => Color.Blue,
                    _ => Color.Black,
                };
            }
        }
    }
}
