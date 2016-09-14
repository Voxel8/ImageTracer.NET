﻿using System;
using System.Collections.Generic;
using System.Linq;
using ImageTracerNet.Extensions;
using ImageTracerNet.OptionTypes;
using PointCalculation = System.Func<double, double, double, double, double>;

namespace ImageTracerNet
{
    internal static class Segmentation
    {
        private static bool Fit(Func<int, double> distanceFunction, double threshold, int initialPathIndex, Func<int, bool> pathCondition, Func<int, int> pathStep, ref int errorPoint)
        {
            var pathIndices = EnumerableExtensions.ForAsRange(initialPathIndex, pathCondition, pathStep);
            var distancesAndIndices = pathIndices.Select(i => new { Index = i, Distance = distanceFunction(i) }).ToList();

            // If this is true, the segment is not this line type.
            if (distancesAndIndices.Any(di => di.Distance > threshold))
            {
                errorPoint = distancesAndIndices.Aggregate(new { Index = errorPoint, Distance = (double)0 },
                    (errorDi, nextDi) => nextDi.Distance > errorDi.Distance ? nextDi : errorDi).Index;
                return false;
            }

            return true;
        }

        // 5.2. Fit a straight line on the sequence
        private static double[] FitLine(List<InterpolationPoint> path, Tracing tracingOptions, int seqStart, int seqEnd, int seqLength, out int errorPoint)
        {
            var pathLength = path.Count;
            var vx = (path[seqEnd].X - path[seqStart].X) / seqLength;
            var vy = (path[seqEnd].Y - path[seqStart].Y) / seqLength;
            Func<int, double> distanceFunction = i =>
            {
                var pl = i - seqStart;
                pl += pl < 0 ? pathLength : 0;
                var px = path[seqStart].X + vx*pl;
                var py = path[seqStart].Y + vy*pl;

                return (path[i].X - px)*(path[i].X - px) + (path[i].Y - py)*(path[i].Y - py);
            };

            errorPoint = seqStart;
            var isLine = Fit(distanceFunction, tracingOptions.LTres, (seqStart + 1) % pathLength, i => i != seqEnd, i => (i + 1) % pathLength, ref errorPoint);
            return isLine ? new[]
            {
                1.0,
                path[seqStart].X,
                path[seqStart].Y,
                path[seqEnd].X,
                path[seqEnd].Y,
                0.0,
                0.0
            } : null;
        }

        //private static Func<int, Point<double>> CreateSplineMethod(List<InterpolationPoint> path, int sequenceStartIndex, int sequenceEndIndex, int sequenceLength, int errorIndex)
        //{
        //    // Static Term Calculations
        //    Func<double, double> t1Calc = t => (1.0 - t)*(1.0 - t);
        //    Func<double, double> t2Calc = t => 2.0*(1.0 - t)*t;
        //    Func<double, double> t3Calc = t => Math.Pow(t, 2);

        //    // Static Point Calculations
        //    Func<double, double, double, double, double> midPointCalc =
        //        (i, start, end, error) => (t1Calc(i)*start + t3Calc(i)*end - error) / -t2Calc(i);

        //    Func<double, double, double, double, double> finalPointCalc =
        //        (i, start, mid, end) => t1Calc(i)*start + t2Calc(i)*mid + t3Calc(i)*end;

        //    Func<double, Point<double>, Point<double>, Point<double>, Func<double, double, double, double, double>, Point<double>> createPoint =
        //        (i, p1, p2, p3, func) => new Point<double>
        //        {
        //            X = func(i, p1.X, p2.X, p3.X),
        //            Y = func(i, p1.Y, p2.Y, p3.Y)
        //        };

        //    // Create the resulting closure using path data.
        //    var startPoint = path[sequenceStartIndex];
        //    var endPoint = path[sequenceEndIndex];
        //    var errorPoint = path[errorIndex];

        //    Func<int, double> indexCalc = i => (i - sequenceStartIndex) / (double)sequenceLength;
        //    var midPoint = createPoint(indexCalc(errorIndex), startPoint, endPoint, errorPoint, midPointCalc);

        //    return i => createPoint(indexCalc(i), startPoint, midPoint, endPoint, finalPointCalc);
        //}

        private static Point<double> CreatePoint(double pseudoIndex, Point<double> first, Point<double> second, Point<double> third, bool isMidPoint)
        {
            // Static Term Calculations
            Func<double, double> t1Calc = t => (1.0 - t) * (1.0 - t);
            Func<double, double> t2Calc = t => 2.0 * (1.0 - t) * t;
            Func<double, double> t3Calc = t => Math.Pow(t, 2);

            // Static Point Calculations
            PointCalculation midPointCalc =
                (i, start, end, error) => (t1Calc(i) * start + t3Calc(i) * end - error) / -t2Calc(i);

            PointCalculation finalPointCalc =
                (i, start, mid, end) => t1Calc(i) * start + t2Calc(i) * mid + t3Calc(i) * end;

            Func<double, Point<double>, Point<double>, Point<double>, PointCalculation, Point<double>> createPoint =
                (i, p1, p2, p3, func) => new Point<double>
                {
                    X = func(i, p1.X, p2.X, p3.X),
                    Y = func(i, p1.Y, p2.Y, p3.Y)
                };

            return createPoint(pseudoIndex, first, second, third, isMidPoint ? midPointCalc : finalPointCalc);
        }

        // 5.4. Fit a quadratic spline through this point, measure errors on every point in the sequence
        // helpers and projecting to get control point
        private static double[] FitSpline(List<InterpolationPoint> path, Tracing tracingOptions, int sequenceStartIndex, int sequenceEndIndex, int sequenceLength, ref int errorIndex)
        {

            //Func<int, int, int, double> tCalc = (current, start, length) => (current - start) / (double)length;
            //Func<double, double> t1Calc = t => (1.0 - t)*(1.0 - t);
            //Func<double, double> t2Calc = t => 2.0 * (1.0 - t) * t;
            //Func<double, double> t3Calc = t => t * t;

            //Func<double, double, double, double, double> partialPointDistance = 
            //    (t, start, end, error) => (t1Calc(t)*start + t3Calc(t)* end - error) / -t2Calc(t);

            //var partial = tCalc(errorPoint, seqStart, seqLength);
            //var cpx = partialPointDistance(partial, path[seqStart].X, path[seqEnd].X, path[errorPoint].X);
            //var cpy = partialPointDistance(partial, path[seqStart].Y, path[seqEnd].Y, path[errorPoint].Y);


            // Static Term Calculations
            //Func<double, double> t1Calc = t => (1.0 - t) * (1.0 - t);
            //Func<double, double> t2Calc = t => 2.0 * (1.0 - t) * t;
            //Func<double, double> t3Calc = t => Math.Pow(t, 2);

            //// Static Point Calculations
            //PointCalculation midPointCalc =
            //    (i, start, end, error) => (t1Calc(i) * start + t3Calc(i) * end - error) / -t2Calc(i);

            //PointCalculation finalPointCalc =
            //    (i, start, mid, end) => t1Calc(i) * start + t2Calc(i) * mid + t3Calc(i) * end;

            //Func<double, Point<double>, Point<double>, Point<double>, PointCalculation, Point<double>> createPoint =
            //    (i, p1, p2, p3, func) => new Point<double>
            //    {
            //        X = func(i, p1.X, p2.X, p3.X),
            //        Y = func(i, p1.Y, p2.Y, p3.Y)
            //    };

            // Create the spline closure using path data.
            var startPoint = path[sequenceStartIndex];
            var endPoint = path[sequenceEndIndex];
            var errorPoint = path[errorIndex];

            Func<int, double> pseudoIndexCalc = i => (i - sequenceStartIndex) / (double)sequenceLength;
            var midPoint = CreatePoint(pseudoIndexCalc(errorIndex), startPoint, endPoint, errorPoint, true);
            //Func<int, Point<double>> splineFunction = i => CreatePoint(pseudoIndexCalc(i), startPoint, midPoint, endPoint, false);

            //var splineFunction = CreateSplineMethod(path, sequenceStartIndex, sequenceEndIndex, sequenceLength, errorIndex);
            Func<int, double> distanceFunction = i =>
            {
                //var full = (i - seqStart) / (double)seqLength;
                //var px = t1 * path[seqStart].X + t2 * cpx + t3 * path[seqEnd].X;
                //var py = t1 * path[seqStart].Y + t2 * cpy + t3 * path[seqEnd].Y;
                var point = CreatePoint(pseudoIndexCalc(i), startPoint, midPoint, endPoint, false);
                return Math.Pow(path[i].X - point.X, 2) + Math.Pow(path[i].Y - point.Y, 2);
            };
            // Check every point
            var isSpline = Fit(distanceFunction, tracingOptions.QTres, sequenceStartIndex + 1, i => i != sequenceEndIndex, i => (i + 1) % path.Count, ref errorIndex);
            return isSpline ? new[]
            {
                2.0,
                startPoint.X,
                startPoint.Y,
                midPoint.X,
                midPoint.Y,
                endPoint.X,
                endPoint.Y
            } : null;
        }

        // 5.2. - 5.6. recursively fitting a straight or quadratic line segment on this sequence of path nodes,
        // called from tracepath()
        public static List<double[]> Fit(List<InterpolationPoint> path, Tracing tracingOptions, int seqStart, int seqEnd)
        {
            var segment = new List<double[]>();
            var pathLength = path.Count;
            // return if invalid seqEnd
            if ((seqEnd > pathLength) || (seqEnd < 0))
            {
                return segment;
            }

            var seqLength = seqEnd - seqStart;
            seqLength += seqLength < 0 ? pathLength : 0;

            int errorPoint;
            var lineResult = FitLine(path, tracingOptions, seqStart, seqEnd, seqLength, out errorPoint);
            if (lineResult != null)
            {
                segment.Add(lineResult);
                return segment;
            }

            // 5.3. If the straight line fails (an error>ltreshold), find the point with the biggest error
            var fitPoint = errorPoint;
            var splineResult = FitSpline(path, tracingOptions, seqStart, seqEnd, seqLength, ref errorPoint);
            if (splineResult != null)
            {
                segment.Add(splineResult);
                return segment;
            }

            // 5.5. If the spline fails (an error>qtreshold), find the point with the biggest error,
            var splitPoint = (fitPoint + errorPoint) / 2;

            // 5.6. Split sequence and recursively apply 5.2. - 5.6. to startpoint-splitpoint and splitpoint-endpoint sequences
            segment = Fit(path, tracingOptions, seqStart, splitPoint);
            segment.AddRange(Fit(path, tracingOptions, splitPoint, seqEnd));
            return segment;
        }
    }
}
