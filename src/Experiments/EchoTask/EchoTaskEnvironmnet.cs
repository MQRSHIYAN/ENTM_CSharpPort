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
        public override int InputCount => 8;
        public override int OutputCount => 8;
        public override double[] InitialObservation => _sequence[0];
        public override double CurrentScore => _score;
        public override double MaxScore => _sequence.Length;
        public override double NormalizedScore => _score/MaxScore;
        public override bool IsTerminated => _step >= TotalTimeSteps;
        public override int TotalTimeSteps => _sequence.Length;
        public override int MaxTimeSteps => TotalTimeSteps;
        public override int NoveltyVectorLength { get; }
        public override int NoveltyVectorDimensions { get; }
        public override int MinimumCriteriaLength { get; }

        private double[][] _sequence;
        private int _step;
        private double _score;
        private readonly int _sequenceLength;
        private EnvironmentTimeStep _prevTimeStep;

        public EchoTaskEnvironmnet()
        {
            _sequenceLength = 10;
        }

        public override void ResetIteration()
        {
            CreateSequence();
            _step = 1;
            _score = 0d;
        }

        public override double[] PerformAction(double[] action)
        {
            double[] prev = _sequence[_step - 1];

            var thisScore = Evaluate(action, prev);
            _score += thisScore;
                        
            var result = _sequence[_step];
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
            int length = SealedRandom.Next(1,_sequenceLength);
            _sequence = new double[length][];
            for (int i = 0; i < length; i++)
            {
                _sequence[i] = new double[OutputCount];
                for (int j = 0; j < OutputCount; j++)
                {
                    _sequence[i][j] = SealedRandom.NextDouble();
                }
            }
        }

        private double Evaluate(double[] src, double[] target)
        {
            double sum = 0;
            for (int i = 0; i < src.Length; i++)
            {
                var dif = Math.Abs(src[i] - target[i]);
                sum += dif < .01 ? 1  : 0;
            }
            return sum/src.Length;
        }
    }
}
