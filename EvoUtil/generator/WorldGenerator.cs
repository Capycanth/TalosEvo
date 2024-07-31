using ChekhovsUtil.log;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TalosEvo.Core.enumeration;
using TalosEvo.Core.World;
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

        private static readonly int[][] Directions = new int[][]
        {
            new int[] { 1, 0 }, new int[] { -1, 0 }, // Horizontal
            new int[] { 0, 1 }, new int[] { 0, -1 }, // Vertical
            new int[] { 1, 1 }, new int[] { -1, -1 }, // Diagonal
            new int[] { 1, -1 }, new int[] { -1, 1 }
        };

        public float[,] HeightMap { get; private set; }
        public float[,] TemperatureMap { get; private set; }
        public float[,] RainfallMap { get; private set; }
        public bool[,] LakeMap { get; private set; }
        public float[,] RiverMap { get; private set; }
        public List<Region> Regions { get; private set; }
        public Biome[,] BiomeMap { get; private set; }

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
            LakeMap = new bool[width, height];
            RiverMap = new float[width, height];
            Regions = new List<Region>();
            BiomeMap = new Biome[width, height];

            GenerateHeightmap();
            SimulateErosion(HeightMap);
            GenerateTemperatureAndRainfallMaps();
            SimulateErosion(RainfallMap);
            SimulateErosion(TemperatureMap);
            IdentifyLakes();
            GenerateRivers(10);
            SmoothRiverEdges();

            GenerateBiomeMap();
            IdentifyRegions(700);

            stopwatch.Stop();
            logger.Debug($"WorldGenerator initialization took {stopwatch.ElapsedMilliseconds} ms.");

            stopwatch.Restart();

            CalculateNoiseStatistics(HeightMap, "HeightMap");
            CalculateNoiseStatistics(TemperatureMap, "TempMap");
            CalculateNoiseStatistics(RainfallMap, "RainMap");
            CalculateNoiseStatistics(LakeMap, "LakeMap");

            stopwatch.Stop();
            logger.Debug($"NoiseArray statistics took {stopwatch.ElapsedMilliseconds} ms.");
            stopwatch.Reset();
        }

        private void GenerateHeightmap()
        {
            Parallel.For(0, width, x =>
            {
                for (int y = 0; y < height; y++)
                {
                    float nx = x / (float)width - 0.5f;
                    float ny = y / (float)height - 0.5f;

                    HeightMap[x, y] = noise.Noise(nx * 5, ny * 5, persistence: 0.5f, amplitudeScaling: 3f, frequencyScaling: 1.1f);
                }
            });
            HeightMap = NormalizeNoise(HeightMap, "HeightMap");
        }

        private void SimulateErosion(float[,] map)
        {
            int iterations = 7;  // Number of erosion iterations
            for (int i = 0; i < iterations; i++)
            {
                Parallel.For(1, width - 1, x =>
                {
                    for (int y = 1; y < height - 1; y++)
                    {
                        // Simple smoothing: average out values with neighbors
                        float sum = 0;
                        int count = 0;

                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                sum += map[x + dx, y + dy];
                                count++;
                            }
                        }

                        map[x, y] = sum / count;
                    }
                });
            }
        }

        private void GenerateTemperatureAndRainfallMaps()
        {
            Parallel.For(0, width, x =>
            {
                for (int y = 0; y < height; y++)
                {
                    float nx = x / (float)width;
                    float ny = y / (float)height;

                    TemperatureMap[x, y] = noise.Noise(nx * 5 + 100, ny * 5 + 100, persistence: 0.8f, amplitudeScaling: 3f, frequencyScaling: 0.3f);
                    RainfallMap[x, y] = noise.Noise(nx * 5 + 50, ny * 5 + 50, persistence: 0.4f, amplitudeScaling: 3f, frequencyScaling: 0.6f);
                }
            });
            TemperatureMap = NormalizeNoise(TemperatureMap, "TempMap");
            RainfallMap = NormalizeNoise(RainfallMap, "RainMap");
        }

        private void IdentifyLakes()
        {
            Parallel.For(0, width, x =>
            {
                for (int y = 0; y < height; y++)
                {
                    if (HeightMap[x, y] < 0.3f) // Arbitrary threshold for lakes
                    {
                        LakeMap[x, y] = true;
                    }
                }
            });
        }

        private void GenerateRivers(int numberOfRivers)
        {
            Parallel.For(0, numberOfRivers, i =>
            {
                int startX = random.Next(width);
                int startY = random.Next(height);

                while (HeightMap[startX, startY] < 0.7f)
                {
                    startX = random.Next(width);
                    startY = random.Next(height);
                }

                GenerateRiver(startX, startY, random.Next(500, 800)); // Start river generation from high elevation
            });
        }

        private void GenerateRiver(int startX, int startY, int initialLength)
        {
            int x = startX;
            int y = startY;
            int remainingLength = initialLength;
            float waterLevel = HeightMap[x, y] + 0.01f;

            while (remainingLength > 0)
            {
                if (x < 0 || x >= width || y < 0 || y >= height) break;

                MarkRiver(x, y);

                // Find the steepest descent with a weighted approach to avoid sharp angles
                int nextX = x;
                int nextY = y;
                float minHeight = waterLevel;
                bool foundDescent = false;

                // Randomize the direction order
                var shuffledDirections = Directions.OrderBy(d => random.Next()).ToArray();

                foreach (var dir in shuffledDirections)
                {
                    int nx = x + dir[0];
                    int ny = y + dir[1];
                    if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                    {
                        float heightDiff = HeightMap[x, y] - HeightMap[nx, ny];
                        float distance = Math.Abs(dir[0]) + Math.Abs(dir[1]);
                        float weightedHeight = heightDiff / distance;

                        if (HeightMap[nx, ny] < minHeight || (HeightMap[nx, ny] == minHeight && weightedHeight > heightDiff))
                        {
                            minHeight = HeightMap[nx, ny];
                            nextX = nx;
                            nextY = ny;
                            foundDescent = true;
                        }
                    }
                }

                if (!foundDescent)
                {
                    // Increase the water level slightly if no descent is found
                    waterLevel += 0.01f;
                    remainingLength -= 1; // Each 0.01f water level increase costs 1 unit of length
                    continue;
                }

                x = nextX;
                y = nextY;
                waterLevel = Math.Max(waterLevel, HeightMap[x, y] + 0.01f);
                remainingLength--;

                // Create branches for any lower areas around the current water level
                CreateBranches(x, y, waterLevel, remainingLength);
            }
        }

        private void CreateBranches(int x, int y, float waterLevel, int remainingLength)
        {
            foreach (var dir in Directions.OrderBy(d => random.Next()))
            {
                int nx = x + dir[0];
                int ny = y + dir[1];
                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                {
                    if (HeightMap[nx, ny] < waterLevel && RiverMap[nx, ny] == 0)
                    {
                        GenerateRiver(nx, ny, remainingLength / 2); // Split remaining length among branches
                    }
                }
            }
        }

        private void MarkRiver(int x, int y)
        {
            RiverMap[x, y] += 1.0f;
        }

        private void SmoothRiverEdges()
        {
            float[,] smoothedRiverMap = new float[width, height];

            Parallel.For(1, width - 1, x =>
            {
                for (int y = 1; y < height - 1; y++)
                {
                    if (RiverMap[x, y] > 0)
                    {
                        float sum = 0;
                        int count = 0;

                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                int nx = x + dx;
                                int ny = y + dy;

                                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                                {
                                    sum += RiverMap[nx, ny];
                                    count++;
                                }
                            }
                        }

                        smoothedRiverMap[x, y] = sum / count;
                    }
                }
            });

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    RiverMap[x, y] = smoothedRiverMap[x, y];
                }
            }
        }

        public float[,] NormalizeNoise(float[,] noiseArray, string arrayName)
        {
            if (noiseArray == null || noiseArray.Length == 0)
            {
                logger.Error($"Empty noise array in Normalize method: {arrayName}");
            }

            int width = noiseArray.GetLength(0);
            int height = noiseArray.GetLength(1);

            float min = float.MaxValue;
            float max = float.MinValue;

            // Find min and max values
            Parallel.For(0, width, x =>
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
                }
            });

            float range = max - min;
            float[,] normalizedNoise = new float[width, height];

            // Normalize the values
            Parallel.For(0, width, x =>
            {
                for (int y = 0; y < height; y++)
                {
                    normalizedNoise[x, y] = (noiseArray[x, y] - min) / range;
                }
            });

            return normalizedNoise;
        }

        public Biome[,] GenerateBiomeMap()
        {
            Parallel.For(0, width, x =>
            {
                for (int y = 0; y < height; y++)
                {
                    BiomeMap[x, y] = EvoConstants.BiomeLookup.GetBiome(
                         TemperatureMap[x, y],
                         RainfallMap[x, y],
                         HeightMap[x, y],
                         RiverMap[x, y] > 0,
                         LakeMap[x, y]);
                }
            });

            return BiomeMap;
        }

        private void IdentifyRegions(int minArea)
        {
            bool[,] visited = new bool[width, height];
            List<Region> smallRegions = new List<Region>();

            Parallel.For(0, width, x =>
            {
                for (int y = 0; y < height; y++)
                {
                    if (!visited[x, y])
                    {
                        Region region = new Region(BiomeMap[x, y]);
                        FloodFill(x, y, region, visited);

                        // Exempt regions containing river tiles
                        bool containsRiver = region.Cells.Any(cell => RiverMap[cell.x, cell.y] > 0);

                        if (region.Cells.Count < minArea && !containsRiver)
                        {
                            lock (smallRegions)
                            {
                                smallRegions.Add(region);
                            }
                        }
                        else
                        {
                            lock (Regions)
                            {
                                Regions.Add(region);
                            }
                        }
                    }
                }
            });

            foreach (var smallRegion in smallRegions.ToList())
            {
                CombineSmallRegion(smallRegion, minArea);
            }
        }


        private void CombineSmallRegion(Region smallRegion, int minArea)
        {
            Region nearestRegion = null;
            double nearestDistance = double.MaxValue;

            foreach (var smallCell in smallRegion.BarrierCells)
            {
                foreach (var region in Regions)
                {
                    if (region.BiomeType == smallRegion.BiomeType)
                    {
                        foreach (var cell in region.BarrierCells)
                        {
                            double distance = Math.Sqrt(Math.Pow(cell.x - smallCell.x, 2) + Math.Pow(cell.y - smallCell.y, 2));
                            if (distance < nearestDistance)
                            {
                                nearestDistance = distance;
                                nearestRegion = region;
                            }
                        }
                    }
                }
            }

            if (nearestRegion != null)
            {
                nearestRegion.Cells.AddRange(smallRegion.Cells);
                nearestRegion.BarrierCells.AddRange(smallRegion.BarrierCells);
                Regions.Remove(smallRegion); // Remove small region after combining

                if (nearestRegion.Cells.Count < minArea)
                {
                    CombineSmallRegion(nearestRegion, minArea);
                }
            }
            else
            {
                Regions.Add(smallRegion); // If no nearby region found, add as is
            }
        }



        private void FloodFill(int startX, int startY, Region region, bool[,] visited)
        {
            Queue<(int x, int y)> queue = new Queue<(int x, int y)>();
            queue.Enqueue((startX, startY));

            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();
                if (x < 0 || x >= width || y < 0 || y >= height || visited[x, y] || BiomeMap[x, y] != region.BiomeType)
                    continue;

                bool isBarrier = false;
                visited[x, y] = true;
                region.AddCell(x, y, isBarrier);

                foreach (var dir in Directions)
                {
                    int nx = x + dir[0];
                    int ny = y + dir[1];
                    if (nx < 0 || nx >= width || ny < 0 || ny >= height || BiomeMap[nx, ny] != region.BiomeType)
                    {
                        isBarrier = true;
                    }
                    else if (!visited[nx, ny])
                    {
                        queue.Enqueue((nx, ny));
                    }
                }

                if (isBarrier)
                {
                    region.AddCell(x, y, true);
                }
            }
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

            Parallel.For(0, width, x =>
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
            });

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

            Parallel.For(0, width, x =>
            {
                for (int y = 0; y < height; y++)
                {
                    bool value = noiseArray[x, y];
                    trueCount += value ? 1 : 0;
                    count++;
                }
            });

            int falseCount = count - trueCount;
            string message = $"{arrayName}: total: |{count}| true: |{trueCount}| false: |{falseCount}|";
            logger.Debug(message);
        }
    }
}
