using System;
using System.Xml;
using ENTM.Base;
using ENTM.Replay;

namespace ENTM.Experiments.WalkerTask
{
    internal class WalkerTaskEvaluator : BaseEvaluator<WalkerTaskEnvironment, DefaultController>
    {
        public override void Initialize(XmlElement properties)
        {
           
        }

        public override int Iterations => 10;
        public override int MaxScore => 1;
        public override int EnvironmentInputCount => Environment.InputCount;
        public override int EnvironmentOutputCount => Environment.OutputCount;
        public override int ControllerInputCount => 0;
        public override int ControllerOutputCount => 0;
        public override int NoveltyVectorDimensions { get; }
        public override int NoveltyVectorLength { get; }
        public override int MinimumCriteriaLength { get; }
        protected override void EvaluateObjective(DefaultController controller, int iterations, ref EvaluationInfo evaluation)
        {
            double totalScore = 0;
            for (int i = 0; i < iterations; i++)
            {
                Reset();
                double[] environmnentOutput = Environment.InitialObservation;
                while (!Environment.IsTerminated)
                {
                    var envInput = Controller.ActivateNeuralNetwork(environmnentOutput);
                    environmnentOutput = Environment.PerformAction(envInput);
                }
                totalScore += Environment.NormalizedScore;
            }
            if (totalScore == Double.NaN)
            {
                throw new Exception();
            }
            evaluation.ObjectiveFitness = totalScore / iterations;
        }

        protected override void EvaluateNovelty(DefaultController controller, ref EvaluationInfo evaluation)
        {
            throw new System.NotImplementedException();
        }

        protected override void EvaluateRecord(DefaultController controller, int iterations, ref EvaluationInfo evaluation)
        {
            Reset();
            Recorder = new Recorder();
            Recorder.Start();
            Environment.RecordTimeSteps = true;
            Recorder.Record(Environment.InitialTimeStep);

            double[] environmnentOutput = Environment.InitialObservation;
            while (!Environment.IsTerminated)
            {
                var envInput = Controller.ActivateNeuralNetwork(environmnentOutput);
                environmnentOutput = Environment.PerformAction(envInput);
                Recorder.Record(Environment.PreviousTimeStep);
            }

            evaluation.ObjectiveFitness = Environment.NormalizedScore; ;
        }

        protected override void SetupTest()
        {
            Environment.RandomSeed = System.Environment.TickCount;
        }

        protected override void TearDownTest()
        {
            Environment.RandomSeed = 0;
        }

        protected override WalkerTaskEnvironment NewEnvironment()
        {
            return new WalkerTaskEnvironment();
        }

        protected override DefaultController NewController()
        {
            return new DefaultController();
        }
    }
}