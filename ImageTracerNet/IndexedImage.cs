﻿using System.Drawing;
using System.Linq;
using ImageTracerNet.Extensions;
using ImageTracerNet.OptionTypes;
using ImageTracerNet.Palettes;
using TriListDoubleArray = System.Collections.Generic.List<System.Collections.Generic.List<System.Collections.Generic.List<double[]>>>;

namespace ImageTracerNet
{
    // Container for the color-indexed image before and tracedata after vectorizing
    internal class IndexedImage
    {
        public int Width { get; }
        public int Height { get; }
        // array[x][y] of palette colors
        public int[][] Array { get; }
        // array[palettelength][4] RGBA color palette
        public byte[][] Palette { get;  }
        // tracedata
        public TriListDoubleArray Layers { set; get; }

        public IndexedImage(int[][] array, byte[][] palette)
        {
            Array = array;
            Palette = palette;
            // Color quantization adds +2 to the original width and height
            Width = array[0].Length - 2;
            Height = array.Length - 2;
        }

        // Creating indexed color array arr which has a boundary filled with -1 in every direction
        // Example: 4x4 image becomes a 6x6 matrix:
        // -1 -1 -1 -1 -1 -1
        // -1  0  0  0  0 -1
        // -1  0  0  0  0 -1
        // -1  0  0  0  0 -1
        // -1  0  0  0  0 -1
        // -1 -1 -1 -1 -1 -1
        private static int[][] CreateIndexedColorArray(int height, int width)
        {
            height += 2;
            width += 2;
            return new int[height][].Initialize(i =>
            i == 0 || i == height - 1
                ? new int[width].Initialize(-1)
                : new int[width].Initialize(-1, 0, width - 1));
        }

        public static IndexedImage Create(ImageData imageData, Color[] colorPalette, ColorQuantization colorQuant)
        {
            var arr = CreateIndexedColorArray(imageData.Height, imageData.Width);
            // Repeat clustering step "cycles" times
            for (var cycleCount = 0; cycleCount < colorQuant.ColorQuantCycles; cycleCount++)
            {
                for (var j = 0; j < imageData.Height; j++)
                {
                    for (var i = 0; i < imageData.Width; i++)
                    {
                        var pixel = imageData.Colors[j * imageData.Width + i];
                        var distance = 256 * 4;
                        var paletteIndex = 0;
                        // find closest color from palette by measuring (rectilinear) color distance between this pixel and all palette colors
                        for (var k = 0; k < colorPalette.Length; k++)
                        {
                            var color = colorPalette[k];
                            // In my experience, https://en.wikipedia.org/wiki/Rectilinear_distance works better than https://en.wikipedia.org/wiki/Euclidean_distance
                            var newDistance = color.CalculateRectilinearDistance(pixel);

                            if (newDistance >= distance) continue;

                            distance = newDistance;
                            paletteIndex = k;
                        }

                        arr[j + 1][i + 1] = paletteIndex;
                    }
                }
            }

            return new IndexedImage(arr, colorPalette.Select(c => c.ToRgbaByteArray()).ToArray());
        }
    }
}
