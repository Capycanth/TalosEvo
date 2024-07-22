using System;
using System.Linq;

namespace TalosEvo.EvoUtil.perlin
{
    public class AdvancedPerlinNoise
    {
        private int[] permutation;
        private int repeat;

        public AdvancedPerlinNoise(int seed, int repeat = -1)
        {
            Random rand = new Random(seed);
            permutation = Enumerable.Range(0, 256).OrderBy(x => rand.Next()).ToArray();
            permutation = permutation.Concat(permutation).ToArray();
            this.repeat = repeat;
        }

        /*
         * Octaves:     refers to the number of noise layers combined to produce the final noise value. 
         *              Each octave is a Perlin noise function with a different frequency and amplitude. 
         *              By summing multiple octaves, the noise pattern becomes more complex and realistic.
         *              
         * Persistence: controls how quickly the amplitudes decrease with each successive octave. 
         *              A higher persistence value means the amplitudes decrease more slowly, giving more weight to higher-frequency noise layers. 
         *              It affects the smoothness of the resulting noise.
         *              
         * Lacunarity: controls how quickly the frequencies increase with each successive octave. 
         *             A higher lacunarity value means the frequencies increase more rapidly, resulting in finer details being added to the noise pattern. 
         *             It determines the "gap" between successive frequencies.
         */
        public float Noise(float x, float y, int octaves = 8, float persistence = 0.5f, float lacunarity = 2.0f, float amplitudeScaling = 1.0f, float frequencyScaling = 1)
        {

            /* The total is the final noise value obtained by summing the contributions from all octaves. 
             * Each octave contributes a noise value that is scaled by its amplitude. 
             * The total represents the cumulative effect of these multiple noise layers. */
            float total = 0;
            /* The frequency determines the scale of the noise. 
             * Higher frequencies result in more rapid changes and finer details in the noise pattern, while lower frequencies result in smoother, broader features. 
             * Frequency typically doubles with each successive octave to add more detail. */
            float frequency = frequencyScaling;
            /* The amplitude determines the influence or weight of each noise layer. 
             * Higher amplitudes result in larger contributions from that octave to the final noise value. 
             * Amplitude typically decreases with each successive octave to ensure higher-frequency noise has less impact, making the overall noise smoother. */
            float amplitude = amplitudeScaling;
            float maxValue = 0;

            for (int i = 0; i < octaves; i++)
            {
                total += SingleNoise(x * frequency, y * frequency) * amplitude;

                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }

            return total / maxValue;
        }

        // Will return a float between 0 and 1
        private float SingleNoise(float x, float y)
        {
            if (repeat > 0)
            {
                x %= repeat;
                y %= repeat;
            }

            int X = (int)Math.Floor(x) & 255;
            int Y = (int)Math.Floor(y) & 255;

            x -= (float)Math.Floor(x);
            y -= (float)Math.Floor(y);

            float u = Fade(x);
            float v = Fade(y);

            int aa, ab, ba, bb;
            aa = permutation[permutation[X] + Y];
            ab = permutation[permutation[X] + Y + 1];
            ba = permutation[permutation[X + 1] + Y];
            bb = permutation[permutation[X + 1] + Y + 1];

            float res = Lerp(v, Lerp(u, Grad(permutation[aa], x, y), Grad(permutation[ba], x - 1, y)),
                               Lerp(u, Grad(permutation[ab], x, y - 1), Grad(permutation[bb], x - 1, y - 1)));

            return (res + 1) / 2;
        }

        private float Fade(float t)
        {
            return t * t * t * (t * (t * 6 - 15) + 10);
        }

        private float Lerp(float t, float a, float b)
        {
            return a + t * (b - a);
        }

        private float Grad(int hash, float x, float y)
        {
            int h = hash & 3;
            float u = h < 2 ? x : y;
            float v = h < 2 ? y : x;
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }
    }

}
