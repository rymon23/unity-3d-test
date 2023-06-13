using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WFCSystem;

namespace ProceduralBase
{
    [System.Serializable]
    public class LayerdNoise
    {
        public static float Calculate_NoiseHeightForCoordinate(int indexX, int indexZ, float terrainHeight, List<LayeredNoiseOption> layeredNoiseOptions)
        {
            float initialValue = float.MaxValue;
            float ratio = 1f;
            float sum = 0f;
            int count = 0;
            foreach (LayeredNoiseOption noiseOption in layeredNoiseOptions)
            {
                float noise = Calculate_NoiseHeightForCoordinate(indexX, indexZ, noiseOption.noise.fastNoise, noiseOption.persistence, noiseOption.lacunarity);

                if (noiseOption.clampValue) noise = Mathf.Clamp(noise, noiseOption.clampRange.x, noiseOption.clampRange.y);
                float basePosY = (noise * noiseOption.mult) * terrainHeight;

                if (initialValue == float.MaxValue) initialValue = basePosY;
                sum += (basePosY * noiseOption.weight);
                count += (1 * noiseOption.weight);
            }
            float average = (sum / count);

            float finalValue = Mathf.Lerp(initialValue, average, ratio);

            return finalValue;
        }

        public static float Calculate_NoiseForCoordinate(int indexX, int indexZ, LayeredNoiseOption noiseOption)
        {
            float noise = Calculate_NoiseHeightForCoordinate(indexX, indexZ, noiseOption.noise.fastNoise, noiseOption.persistence, noiseOption.lacunarity);

            if (noiseOption.clampValue) noise = Mathf.Clamp(noise, noiseOption.clampRange.x, noiseOption.clampRange.y);
            return noise;
        }

        public static float Calculate_NoiseForCoordinate(int indexX, int indexZ, List<LayeredNoiseOption> layeredNoiseOptions)
        {
            float initialValue = float.MaxValue;
            float ratio = 1f;
            float sum = 0f;
            int count = 0;
            foreach (LayeredNoiseOption noiseOption in layeredNoiseOptions)
            {
                float noise = Calculate_NoiseHeightForCoordinate(indexX, indexZ, noiseOption.noise.fastNoise, noiseOption.persistence, noiseOption.lacunarity);

                if (noiseOption.clampValue) noise = Mathf.Clamp(noise, noiseOption.clampRange.x, noiseOption.clampRange.y);

                if (initialValue == float.MaxValue) initialValue = noise;

                sum += (noise * noiseOption.weight);
                count += (1 * noiseOption.weight);
            }
            float average = (sum / count);
            float finalValue = Mathf.Lerp(initialValue, average, ratio);
            return finalValue;
        }

        private static float Calculate_NoiseHeightForCoordinate(float x, float z, FastNoise fastNoise, float persistence, float lacunarity, int octaves = 4)
        {
            float noiseHeight = 0f;
            float amplitude = 1f;
            float frequency = 1f;

            for (int i = 0; i < octaves; i++)
            {
                float noiseValue = fastNoise.GetNoise(x * frequency, z * frequency);
                noiseHeight += noiseValue * amplitude;

                amplitude *= persistence;
                frequency *= lacunarity;
            }
            return noiseHeight;
        }

        // private static float Calculate_NoiseHeightForCoordinate(float x, float z, FastNoise fastNoise, float persistence, float octaves = 3)
        // {
        //     // Calculate the height of the current point
        //     float noiseHeight = 0;
        //     float amplitude = 1;
        //     // float frequency = 1;

        //     for (int i = 0; i < octaves; i++)
        //     {
        //         float noiseValue = (float)fastNoise.GetNoise(x, z);

        //         noiseHeight += noiseValue * amplitude;
        //         amplitude *= persistence;
        //     }
        //     return noiseHeight;
        // }

        // public static float CalculateNoiseHeightForVertex(int indexX, int indexZ, float terrainHeight, List<FastNoiseUnity> noiseFunctions, float persistence, float octaves, float lacunarity)
        // {
        //     float sum = 0f;
        //     foreach (FastNoiseUnity noise in noiseFunctions)
        //     {
        //         float noiseHeight = GetNoiseHeightValue(indexX, indexZ, noise.fastNoise, persistence, octaves, lacunarity);
        //         float basePosY = noiseHeight * terrainHeight;

        //         sum += basePosY;
        //     }
        //     float average = sum / noiseFunctions.Count;
        //     return average;
        // }
        private static float GetNoiseHeightValue(float x, float z, FastNoise fastNoise, float persistence, float octaves, float lacunarity)
        {
            // Calculate the height of the current point
            float noiseHeight = 0;
            float amplitude = 1;
            // float frequency = 1;

            for (int i = 0; i < octaves; i++)
            {
                float noiseValue = (float)fastNoise.GetNoise(x, z);

                noiseHeight += noiseValue * amplitude;
                amplitude *= persistence;
                // frequency *= lacunarity;
            }
            return noiseHeight;
        }

    }

    [System.Serializable]
    public struct LayeredNoiseOption
    {
        public LayeredNoiseOption(
            FastNoiseUnity _noise,
            int _weight = 1,
            float _persistence = 1f,
            float _lacunarity = 1f,
            float _mult = 1f,
            bool _clampValue = false,
            Vector2 _clampRange = new Vector2()
        )
        {
            noise = _noise;
            weight = _weight;
            persistence = _persistence;
            lacunarity = _lacunarity;
            mult = _mult;
            clampValue = _clampValue;
            clampRange = _clampRange;
        }

        public FastNoiseUnity noise;
        [Range(0.1f, 2f)] public float persistence;
        [Range(-1f, 3f)] public float lacunarity;
        [Range(1, 5)] public int weight;
        [Range(-1f, 5f)] public float mult;
        [Header(" ")]
        public bool clampValue;
        public Vector2 clampRange;
        // public AnimationCurve flattenCurve;

        // [Range(0, 5f)] public float rangeMin;
        // [Range(0, 5f)] public float rangeMax;
    }
}