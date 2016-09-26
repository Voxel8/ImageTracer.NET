﻿using System;

namespace ImageTracerNet.OptionTypes
{
    [Serializable]
    public class SvgRendering
    {
        public double Scale { get; set; } = 1f;
        public double SimplifyTolerance { get; set; } = 0f;
        public int RoundCoords { get; set; } = 1;
        public double LCpr { get; set; } = 0f;
        public double QCpr { get; set; } = 0f;
        public bool Desc { get; set; } = true;
        public bool Viewbox { get; set; } = false;
    }
}
