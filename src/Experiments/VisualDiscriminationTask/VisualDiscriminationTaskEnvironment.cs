using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using ENTM.Base;
using ENTM.Replay;

namespace ENTM.Experiments.VisualDiscriminationTask
{
    class VisualDiscriminationTaskEnvironment : BaseEnvironment
    {
        private double[][,] _images;
        private double _score;
        private int _step;
        private int _smallImageX;
        private int _smallImageY;
        private Tuple<int, int>[] _targetCenters;
        private EnvironmentTimeStep _prevTimeStep;
        private const int XDim = 11;
        private const int YDim = 11;
        public override bool RecordTimeSteps { get; set; }
        public override EnvironmentTimeStep InitialTimeStep
        {
            get
            {
                return _prevTimeStep = new EnvironmentTimeStep(new double[InputCount], InitialObservation, _score);
            }
        }
        public override EnvironmentTimeStep PreviousTimeStep => _prevTimeStep;
        public override IController Controller { get; set; }
        public override int InputCount => XDim*YDim;
        public override int OutputCount => XDim*YDim;
        public override double[] InitialObservation => Flatten2DArray(_images[0]);
        public override double CurrentScore => _score;
        public override double MaxScore => 3*(Math.Pow(XDim-1, 2) + Math.Pow(YDim-1, 2));
        public override double NormalizedScore => _score/TotalTimeSteps;
        public override bool IsTerminated => _step > TotalTimeSteps;
        public override int TotalTimeSteps => 3;
        public override int MaxTimeSteps => TotalTimeSteps;
        public override int NoveltyVectorLength { get; }
        public override int NoveltyVectorDimensions { get; }
        public override int MinimumCriteriaLength { get; }



        public override void ResetIteration()
        {
            CreateImages();
            _step = 1;
            _score = 0d;
        }

        private void CreateImages()
        {
            _images = new double[TotalTimeSteps][,];
            _smallImageX = SealedRandom.Next(0, 11);
            _smallImageY = SealedRandom.Next(0, 11);
            _targetCenters = new Tuple<int, int>[3];
            for (int i = 0; i < TotalTimeSteps; i++)
            {
                _images[i] = GenerateImage(i);
            }
        }

        private double[,] GenerateImage(int i)
        {
            var result = new double[XDim, YDim];
            result[_smallImageX, _smallImageY] = 1;
            int bigImageCenterX, bigImageCenterY;
            switch (i)
            {
                case 0:
                    bigImageCenterX = (_smallImageX + 5) % XDim;
                    bigImageCenterY = _smallImageY;
                    MoveCenter(ref bigImageCenterX, ref bigImageCenterY);
                    break;
                case 1:
                    bigImageCenterX = _smallImageX;
                    bigImageCenterY = (_smallImageY + 5)%YDim;
                    MoveCenter(ref bigImageCenterX, ref bigImageCenterY);
                    break;
                case 2:
                    bigImageCenterX = (_smallImageX + 5)%XDim;
                    bigImageCenterY = (_smallImageY + 5)%YDim;
                    MoveCenter(ref bigImageCenterX, ref bigImageCenterY);
                    break;
                default:
                    throw new Exception("There shouldn't be more than 3 cases for the images.");
            }
            FillImage(bigImageCenterX, bigImageCenterY, result);
            _targetCenters[i] = new Tuple<int, int>(bigImageCenterX, bigImageCenterY);
            return result;
        }

        private static void FillImage(int bigImageCenterX, int bigImageCenterY, double[,] result)
        {
            for (int i = -1; i < 1; i++)
            {
                for (int j = -1; j < 1; j++)
                {
                    result[bigImageCenterX + i,bigImageCenterY + j] = 1;
                }
            }
        }

        private static void MoveCenter(ref int x, ref int y)
        {
            if (y == 0)
            {
                y = 1;
            }
            else if (y == YDim - 1)
            {
                y = YDim - 2;
            }
            if (x == 0)
            {
                x = 1;
            }
            else if (x == XDim - 1)
            {
                x = XDim - 2;
            }
        }

        public override double[] PerformAction(double[] action)
        {
            var guess = GetXYCoordinates(action);
            var thisScore = Evaluate(guess, _targetCenters[_step-1]);
            _score += thisScore;
            var result = _step >= _images.Length ? new double[XDim*YDim] : Flatten2DArray(_images[_step]);
            _step++;
            if (RecordTimeSteps)
            {
                _prevTimeStep = new EnvironmentTimeStep(action, result, thisScore);
            }
            return result;
        }

        private double Evaluate(Tuple<int, int> actual, Tuple<int, int> targetCenter)
        {
            return Math.Pow(targetCenter.Item1 - actual.Item1, 2) + Math.Pow(targetCenter.Item2 - actual.Item2, 2);
        }

        public override void ResetAll()
        {
            ResetRandom();
        }

        public override int RandomSeed { get; set; }

        private double[] Flatten2DArray(double[,] array)
        {
            var result = new double[array.Length];
            var sizeX = array.GetLength(0);
            var sizeY = array.GetLength(1);
            for (int y = 0; y < sizeY; y++)
            {
                for (int x = 0; x < sizeX; x++)
                {
                    result[y*YDim + x] = array[x, y];
                }
            }
            return result;
        }

        private double[,] Fill2DArray(double[] array)
        {
            if (array.Length != XDim*YDim)
            {
                throw new Exception("Array wrong length.");
            }
            var result = new double[XDim, YDim];
            for (int x = 0; x < XDim; x++)
            {
                for (int y = 0; y < YDim; y++)
                {
                    result[x, y] = array[x + y*YDim];
                }
            }
            return result;
        }

        private Tuple<int, int> GetXYCoordinates(double[] action)
        {
            int x = -1;
            var max = double.NegativeInfinity;
            for (int i = 0; i < action.Length; i++)
            {
                if (!(action[i] > max)) continue;
                x = i;
                max = action[i];
            }
            return new Tuple<int, int>(x%YDim, x/YDim);
        }
    }
}