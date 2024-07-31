using System.Collections.Generic;
using TalosEvo.Core.enumeration;

namespace TalosEvo.Core.World
{
    public class Region
    {
        public Biome BiomeType { get; private set; }
        public List<(int x, int y)> Cells { get; private set; }
        public List<(int x, int y)> BarrierCells { get; private set; }

        public Region(Biome biomeType)
        {
            BiomeType = biomeType;
            Cells = new List<(int x, int y)>();
            BarrierCells = new List<(int x, int y)>();
        }

        public void AddCell(int x, int y, bool isBarrier)
        {
            Cells.Add((x, y));
            if (isBarrier)
            {
                BarrierCells.Add((x, y));
            }
        }
    }
}
