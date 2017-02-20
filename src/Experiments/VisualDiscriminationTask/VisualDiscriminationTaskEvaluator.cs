using System;
using System.Xml;
using ENTM.Base;
using ENTM.Replay;

namespace ENTM.Experiments.VisualDiscriminationTask
{
    class VisualDiscriminationTaskEvaluator : BaseEvaluator<VisualDiscriminationTaskEnvironment, DefaultController>
    {
        public override void Initialize(XmlElement properties)
        {
            
        }

        public const double VisualFieldLength = 2.0;
        const double MeanLineSquareRootMeanSquareLength = 0.5772;

        public override int Iterations => 25;
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
            double activationRangeAcc = 0.0;
            for (int i = 0; i < iterations; i++)
            {
                Reset();
                double[] envOutput = Environment.InitialObservation;
                while (!Environment.IsTerminated)
                {
                    var envInput = Controller.ActivateNeuralNetwork(envOutput);
                    envOutput = Environment.PerformAction(envInput);
                }
                totalScore += Environment.NormalizedScore;
            }
            const double threshold = MeanLineSquareRootMeanSquareLength*VisualFieldLength;
            double rmsd = Math.Sqrt(totalScore/iterations);
            if (rmsd > threshold)
            {
                evaluation.ObjectiveFitness = 0.0;
            }
            else
            {
                evaluation.ObjectiveFitness = (threshold - rmsd)*100.0/threshold + activationRangeAcc/7.5;
                
            }
            evaluation.ObjectiveFitness = 1 / (rmsd + 1);
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

            double[] envOutput = Environment.InitialObservation;
            while (!Environment.IsTerminated)
            {
                var envInput = Controller.ActivateNeuralNetwork(envOutput);
                envOutput = Environment.PerformAction(envInput);
                Recorder.Record(Environment.PreviousTimeStep);
            }
            evaluation.ObjectiveFitness = 600 - Environment.NormalizedScore;
        }

        protected override void SetupTest()
        {
            Environment.RandomSeed = System.Environment.TickCount;
        }

        protected override void TearDownTest()
        {
            Environment.RandomSeed = 0;
        }
        protected override VisualDiscriminationTaskEnvironment NewEnvironment()
        {
            return new VisualDiscriminationTaskEnvironment();
        }

        protected override DefaultController NewController()
        {
            return new DefaultController();
        }
    }
}
