﻿using System;
using System.Drawing;
using System.Linq;

namespace ImageTracerNet.Extensions
{
    public static class ColorExtensions
    {
        public static Color[] FromRgbaByteArray(byte[] data)
        {
            return data.Select((comp, i) => new { Color = i / 4, Component = comp })
                .GroupBy(x => x.Color, x => x.Component).Select(comps =>
                    Color.FromArgb(comps.ElementAt(3), comps.ElementAt(0), comps.ElementAt(1), comps.ElementAt(2)))
                .ToArray();
        }

        private static readonly Random Rng = new Random();
        public static Color RandomColor()
        {
            return FromRgbaByteArray(Enumerable.Range(0, 4).Select(i => (byte)Math.Floor(Rng.NextDouble() * 255)).ToArray()).Single();
        }

        //https://en.wikipedia.org/wiki/Rectilinear_distance
        public static int CalculateRectilinearDistance(this Color first, Color second)
        {
            var firstArray = first.ToRgbaByteArray();
            var secondArray = second.ToRgbaByteArray();
            return firstArray.Zip(secondArray, (f, s) => Math.Abs(f - s)).Aggregate((f, s) => f + s) * 4;
        }

        public static byte[] ToRgbaByteArray(this Color[] colors)
        {
            return colors.Select(c => c.ToRgbaByteArray()).SelectMany(b => b).ToArray();
        }

        public static byte[] ToRgbaByteArray(this Color color)
        {
            return new[] { color.R, color.G, color.B, color.A };
        }
    }
}
