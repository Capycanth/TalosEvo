using ChekhovsUtil.log;
using System;
using System.Diagnostics;
using TalosEvo.Core.enumeration;
using TalosEvo.EvoUtil.perlin;

namespace TalosEvo.EvoUtil.generator
{
    public class WorldGenerator
    {
        private int width;
        private int height;
        private AdvancedPerlinNoise noise;
        private readonly Logger logger = new Logger(typeof(WorldGenerator));
        private readonly Random random = new Random();

        public float[,] HeightMap { get; private set; }
        public float[,] TemperatureMap { get; private set; }
        public float[,] RainfallMap { get; private set; }
        public bool[,] RiverMap { get; private set; }
        public bool[,] LakeMap { get; private set; }

        public WorldGenerator(int width, int height, int seed)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            this.width = width;
            this.height = height;
            noise = new AdvancedPerlinNoise(seed);

            HeightMap = new float[width, height];
            TemperatureMap = new float[width, height];
            RainfallMap = new float[width, height];
            RiverMap = new bool[width, height];
            LakeMap = new bool[width, height];

            GenerateMaps();
            GenerateRiversAndLakes();

            stopwatch.Stop();
            logger.Debug($"WorldGenerator initialization took {stopwatch.ElapsedMilliseconds} ms.");

            stopwatch.Restart();

            CalculateNoiseStatistics(HeightMap, "HeightMap");
            CalculateNoiseStatistics(TemperatureMap, "TemperatureMap");
            CalculateNoiseStatistics(RainfallMap, "RainfallMap");
            CalculateNoiseStatistics(RiverMap, "RiverMap");
            CalculateNoiseStatistics(LakeMap, "LakeMap");

            stopwatch.Stop();
            logger.Debug($"NoiseArray statistics took {stopwatch.ElapsedMilliseconds} ms.");
            stopwatch.Reset();
        }

        private void GenerateMaps()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float nx = x / (float)width;
                    float ny = y / (float)height;

                    HeightMap[x, y] = noise.Noise(nx * 5, ny * 5, amplitudeScaling: 2.0f);
                    TemperatureMap[x, y] = noise.Noise(nx * 5 + 100, ny * 5 + 100, amplitudeScaling: 2.0f);
                    RainfallMap[x, y] = noise.Noise(nx * 5 + 50, ny * 5 + 50, amplitudeScaling: 2.0f);
                }
            }
        }

        // TODO: Parametarize generation variables instead of randomize
        private void GenerateRiversAndLakes()
        {
            for (int i = 0; i < random.NextInt64(20, 50); i++) // Random number of rivers
            {
                int startX = random.Next(width);
                int startY = random.Next(height);
                GenerateRiver(startX, startY);
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (HeightMap[x, y] < 0.25f && !RiverMap[x, y]) // Arbitrary threshold for lakes
                    {
                        LakeMap[x, y] = true;
                    }
                }
            }
        }

        private void GenerateRiver(int startX, int startY)
        {
            int x = startX;
            int y = startY;
            for (int i = 0; i < random.NextInt64(200, 1000); i++) // Random River length
            {
                if (x < 0 || x >= width || y < 0 || y >= height) break;

                RiverMap[x, y] = true;

                // Find the steepest descent
                int nextX = x;
                int nextY = y;
                float minHeight = HeightMap[x, y];

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int nx = x + dx;
                        int ny = y + dy;
                        if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                        {
                            if (HeightMap[nx, ny] < minHeight)
                            {
                                minHeight = HeightMap[nx, ny];
                                nextX = nx;
                                nextY = ny;
                            }
                        }
                    }
                }

                if (nextX == x && nextY == y) break; // No descent, stop the river
                x = nextX;
                y = nextY;
            }
        }

        public Biome[,] GenerateBiomeMap()
        {
            Biome[,] biomeMap = new Biome[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    biomeMap[x, y] = EvoConstants.BiomeLookup.GetBiome(
                         TemperatureMap[x, y],
                         RainfallMap[x, y],
                         HeightMap[x, y],
                         RiverMap[x, y],
                         LakeMap[x, y]);
                }
            }

            return biomeMap;
        }

        private void CalculateNoiseStatistics(float[,] noiseArray, string arrayName)
        {
            if (noiseArray == null || noiseArray.Length == 0)
            {
                logger.Error($"Empty noise array in statistics check: {arrayName}");
                return;
            }

            float min = float.MaxValue;
            float max = float.MinValue;
            float sum = 0f;
            int count = 0;

            int width = noiseArray.GetLength(0);
            int height = noiseArray.GetLength(1);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float value = noiseArray[x, y];
                    if (value < min)
                    {
                        min = value;
                    }
                    if (value > max)
                    {
                        max = value;
                    }
                    sum += value;
                    count++;
                }
            }

            float average = sum / count;
            string message = $"{arrayName}: min: |{min:F4}| max: |{max:F4}| avg: |{average:F4}|";
            logger.Debug(message);
        }

        private void CalculateNoiseStatistics(bool[,] noiseArray, string arrayName)
        {
            if (noiseArray == null || noiseArray.Length == 0)
            {
                logger.Error($"Empty noise array in statistics check: {arrayName}");
                return;
            }

            int trueCount = 0;
            int count = 0;

            int width = noiseArray.GetLength(0);
            int height = noiseArray.GetLength(1);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    bool value = noiseArray[x, y];
                    trueCount += value ? 1 : 0;
                    count++;
                }
            }

            int falseCount = count - trueCount;
            string message = $"{arrayName}: total: |{count}| true: |{trueCount}| false: |{falseCount}|";
            logger.Debug(message);
        }
    }
}
