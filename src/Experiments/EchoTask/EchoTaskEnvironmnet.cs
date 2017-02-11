using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ENTM.Base;
using ENTM.Replay;
using ENTM.Utility;

namespace ENTM.Experiments.EchoTask
{
    class EchoTaskEnvironmnet : BaseEnvironment
    {
        public override bool RecordTimeSteps { get; set; }

        public override EnvironmentTimeStep InitialTimeStep
        {
            get
            {
                return _prevTimeStep = new EnvironmentTimeStep(new double[InputCount], _sequence[0], _score);
            }
        }

        public override EnvironmentTimeStep PreviousTimeStep => _prevTimeStep;
        public override IController Controller { get; set; }
        public override int InputCount => 9;
        public override int OutputCount => 9;
        public override double[] InitialObservation => _sequence[0];
        public override double CurrentScore => _score;
        public override double MaxScore => _sequence.Length;
        public override double NormalizedScore => _score/MaxScore;
        public override bool IsTerminated => _step > TotalTimeSteps;
        public override int TotalTimeSteps => _sequence.Length;
        public override int MaxTimeSteps => TotalTimeSteps;
        public override int NoveltyVectorLength { get; }
        public override int NoveltyVectorDimensions { get; }
        public override int MinimumCriteriaLength { get; }

        public bool Generalize { get; set; }
        private double[][] _sequence;
        private int _step;
        private double _score;
        private readonly int _maxSequenceLength;
        private EnvironmentTimeStep _prevTimeStep;

        public EchoTaskEnvironmnet()
        {
            _maxSequenceLength = 10;
            RandomSeed = System.Environment.TickCount;
        }

        public override void ResetIteration()
        {
            CreateSequence();
            _step = 1;
            _score = 0d;
        }

        public override double[] PerformAction(double[] action)
        {
            var thisScore = Evaluate(action, _sequence[_step - 1]);
            _score += thisScore;

            var result = _step >= _sequence.Length ? new double[OutputCount] : _sequence[_step] ;
            _step++;
            
            if (RecordTimeSteps)
            {
                _prevTimeStep = new EnvironmentTimeStep(action, result, thisScore);
            }
            return result;
        }

        public override void ResetAll()
        {
            Debug.DLogHeader("EchoTask ResetAll", true);
            ResetRandom();
        }

        public override int RandomSeed { get; set; }

        private void CreateSequence()
        {
            if (!Generalize)
            {
                _sequence = new double[SealedRandom.Next(1, _maxSequenceLength + 1)][];
                for (int j = 0; j < _sequence.Length; j++)
                {
                    _sequence[j] = new double[OutputCount];
                    for (int i = 0; i < OutputCount; i++)
                    {
                        _sequence[j][i] = SealedRandom.Next(0, 2);
                    }

                }
            }
            else
            {
                _sequence = new double[100][];
                for (int i = 0; i < _sequence.Length; i++)
                {
                    _sequence[i] = new double[99];
                    for (int j = 0; j < _sequence[i].Length; j++)
                    {
                        _sequence[i][j] = SealedRandom.Next(0, 2);
                    }
                }
            }

        }

        private double Evaluate(double[] src, double[] target)
        {
            double sum = 0;
            for (int i = 0; i < src.Length; i++)
            {
                var dif = Math.Abs(src[i] - target[i]);
                sum += dif < 0.25 ? 1 - dif : 0;
            }
            return sum/src.Length;
        }

    }
}
