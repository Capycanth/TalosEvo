using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TalosEvo.Core.enumeration;
using TalosEvo.EvoUtil;
using TalosEvo.EvoUtil.generator;

namespace TalosEvo.Core.World
{
    public class World
    {
        private int width;
        private int height;
        private Biome[,] biomeMap;
        public Rectangle WorldRectangle { get; private set; }
        public Texture2D WorldTexture { get; private set; }

        public World(int width, int height, int seed, GraphicsDevice graphicsDevice)
        {
            WorldGenerator generator = new WorldGenerator(width, height, seed);
            this.width = width;
            this.height = height;
            biomeMap = generator.GenerateBiomeMap();
            WorldTexture = CreateBiomeTexture(graphicsDevice);
            WorldRectangle = new Rectangle(0,0,width,height);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(WorldTexture, WorldRectangle, Color.White);
        }

        private Texture2D CreateBiomeTexture(GraphicsDevice graphicsDevice)
        {
            Texture2D texture = new Texture2D(graphicsDevice, width, height);
            Color[] colorData = new Color[width * height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    colorData[x + y * width] = EvoConstants.BiomeLookup.GetBiomeColor(biomeMap[x, y]);
                }
            }

            texture.SetData(colorData);
            return texture;
        }
    }
}
