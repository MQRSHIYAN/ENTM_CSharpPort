using System;
using ENTM.Base;
using ENTM.Replay;
using ENTM.Utility;

namespace ENTM.Experiments.WalkerTask
{
    public class WalkerTaskEnvironment : BaseEnvironment
    {
        private static readonly Tuple<double, double> TopLeft = new Tuple<double,double>(-15.0,15);
        private static readonly Tuple<double, double> BottomRight = new Tuple<double,double>(15.0,-15);
        private Tuple<double, double> _agentPosition = new Tuple<double, double>(0,0);
        private double _leftRightDistance = Math.Abs(TopLeft.Item1 - BottomRight.Item1);
        private double _topBottomDistance = Math.Abs(TopLeft.Item2 - BottomRight.Item2);
        private EnvironmentTimeStep _previousTimeStep;
        private int _currentTarget;
        private double _currentScore;
        private int _step;

        public override bool RecordTimeSteps { get; set; }
        public override EnvironmentTimeStep InitialTimeStep { get {return _previousTimeStep = new EnvironmentTimeStep(new double[InputCount], InitialObservation, 0);} }

        public override EnvironmentTimeStep PreviousTimeStep
        {
            get { return _previousTimeStep; }
        }

        public override IController Controller { get; set; }
        public override int InputCount => 5;
        public override int OutputCount => 4;
        public override double[] InitialObservation => CalculateEnvironmentOutput(_agentPosition);

        public override double CurrentScore
        {
            get { return _currentScore; }
        }

        public override double MaxScore => 2*TotalTimeSteps;
        public override double NormalizedScore => _currentScore/MaxScore;
        public override bool IsTerminated => _step > TotalTimeSteps;
        public override int TotalTimeSteps => 10;
        public override int MaxTimeSteps => TotalTimeSteps;                       
        public override int NoveltyVectorLength { get; }                
        public override int NoveltyVectorDimensions { get; }            
        public override int MinimumCriteriaLength { get; }              
                                                                        
        public override void ResetIteration()
        {
            var x = SealedRandom.Next(0,2) == 0 ? SealedRandom.NextDouble() : -SealedRandom.NextDouble();
            var y = SealedRandom.Next(0,2) == 0 ? SealedRandom.NextDouble() : -SealedRandom.NextDouble();
            _currentTarget = SealedRandom.Next(0, 4);
            _agentPosition = new Tuple<double, double>(x,y);
            _step = 1;
            _currentScore = 0;
        }

        public override double[] PerformAction(double[] action)
        {
            var move = GetUnitVector(action);
            var targetVector = GetTargetVector(_currentTarget);
            var thisScore = new Tuple<double, double>(0,0).Equals(move) ? 0.0 : GetCosineSimilarity(move, targetVector) + 1; //Score between 0 and 2
            var newPosition = AddVectors(_agentPosition, move);
            if(thisScore == Double.NaN)
                throw new Exception();
            _currentScore += thisScore;
            _agentPosition = newPosition;
            MaybeChangeDirection();
            var result = CalculateEnvironmentOutput(_agentPosition);
            if (RecordTimeSteps)
            {
                _previousTimeStep = new EnvironmentTimeStep(action, result, thisScore);
            }
            _step++;
            return result;
        }



        public override void ResetAll()
        {
            ResetRandom();
        }

        public override int RandomSeed { get; set; }

        private double CalculateDistanceToLeft(Tuple<double, double> pos)
        {
            return Math.Abs(pos.Item1 - TopLeft.Item1)/_leftRightDistance;
        }

        private double CalculateDistaneToRight(Tuple<double, double> pos)
        {
            return Math.Abs(pos.Item1 - BottomRight.Item1)/_leftRightDistance;
        }

        private double CalculateDistanceToTop(Tuple<double, double> pos)
        {
            return Math.Abs(pos.Item2 - TopLeft.Item2)/_topBottomDistance;
        }

        private double CalculateDistanceToBottom(Tuple<double, double> pos)
        {
            return Math.Abs(pos.Item2 - BottomRight.Item2)/_topBottomDistance;
        }

        private double[] CalculateEnvironmentOutput(Tuple<double, double> agentPos)
        {
            return new []
            {
                CalculateDistanceToTop(agentPos),
                CalculateDistaneToRight(agentPos),
                CalculateDistanceToBottom(agentPos),
                CalculateDistanceToLeft(agentPos),
                _currentTarget/3.0
            };
        }

        private Tuple<double, double> GetUnitVector(double[] input)
        {
            var deltaX = input[1] - input[3];
            var deltaY = input[0] - input[2];
            if (deltaX == 0.0 && deltaY == 0.0)
            {
                return new Tuple<double, double>(0,0);
            }
            var length = Math.Sqrt(deltaX*deltaX + deltaY*deltaY);
            return new Tuple<double, double>(deltaX/length, deltaY/length);
        }

        private Tuple<double, double> AddVectors(Tuple<double, double> vector1, Tuple<double, double> vector2)
        {            
            return new Tuple<double, double>(vector1.Item1+vector2.Item1, vector1.Item2 +vector2.Item2);
        }

        private Tuple<double, double> GetTargetVector(int targetPos)
        {
            switch (targetPos)
            {
                case 0:
                    return AddVectors(TopLeft, _agentPosition);
                case 1:
                    return AddVectors(new Tuple<double, double>(TopLeft.Item2, BottomRight.Item1), _agentPosition);
                case 2:
                    return AddVectors(BottomRight, _agentPosition);
                case 3:
                    return AddVectors(new Tuple<double, double>(TopLeft.Item1, BottomRight.Item2), _agentPosition);
                default:
                    throw new ArgumentException();
            }
        }

        private double GetCosineSimilarity(Tuple<double, double> vector1, Tuple<double, double> vector2)
        {
            return GetDotProduct(vector1, vector2)/(GetVectorLength(vector1)*GetVectorLength(vector2));
        }

        private double GetDotProduct(Tuple<double, double> vector1, Tuple<double, double> vector2)
        {
            return vector1.Item1*vector2.Item1 + vector1.Item2 * vector2.Item2;
        }

        private double GetVectorLength(Tuple<double, double> vector)
        {
            return Math.Sqrt(vector.Item1*vector.Item1 + vector.Item2 + vector.Item2);
        }

        private void MaybeChangeDirection()
        {
            if (SealedRandom.NextDouble() < 0.25)
            {
                _currentTarget = SealedRandom.Next(0, 4);
            }
        }

    }
}