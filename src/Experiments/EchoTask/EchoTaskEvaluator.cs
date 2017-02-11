using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using ENTM.Base;
using ENTM.Replay;

namespace ENTM.Experiments.EchoTask
{
    class EchoTaskEvaluator : BaseEvaluator<EchoTaskEnvironmnet, DefaultController>
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
        public override int MinimumCriteriaLength => 0;

        protected override void EvaluateObjective(DefaultController controller, int iterations, ref EvaluationInfo evaluation)
        {
            double totalScore = 0;

            for (int i = 0; i < iterations; i++)
            {
                Reset();
                double[] environmnentOutput =  Environment.InitialObservation;
                while (!Environment.IsTerminated)
                {
                    var envInput = Controller.ActivateNeuralNetwork(environmnentOutput);
                    environmnentOutput = Environment.PerformAction(envInput);
                }
                totalScore += Environment.NormalizedScore;
            }
            evaluation.ObjectiveFitness = totalScore/iterations;
        }

        protected override void EvaluateNovelty(DefaultController controller, ref EvaluationInfo evaluation)
        {
            throw new NotImplementedException();
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

        protected override void SetupGeneralizationTest()
        {
            Environment.Generalize = true;
        }

        protected override void TearDownTest()
        {
            Environment.RandomSeed = 0;
            Environment.Generalize = false;
        }
        protected override EchoTaskEnvironmnet NewEnvironment()
        {
            return new EchoTaskEnvironmnet();
        }

        protected override DefaultController NewController()
        {
            return new DefaultController();
        }
    }
}
