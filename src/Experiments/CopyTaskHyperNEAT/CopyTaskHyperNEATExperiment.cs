using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ENTM.Base;
using ENTM.Experiments.CopyTask;
using ENTM.Experiments.SeasonTask;
using ENTM.TuringMachine;
using SharpNeat.Core;
using SharpNeat.Decoders;
using SharpNeat.Decoders.HyperNeat;
using SharpNeat.Genomes.Neat;
using SharpNeat.Network;
using SharpNeat.Phenomes;

namespace ENTM.Experiments.CopyTaskHyperNEAT
{

    /// <summary>
    /// Heavily inspired by http://www.nashcoding.com/2010/10/29/tutorial-%E2%80%93-evolving-neural-networks-with-sharpneat-2-part-3/
    /// </summary>
    public class CopyTaskHyperNEATExperiment : BaseExperiment<CopyTaskEvaluator, CopyTaskEnvironment, TuringController>
    {
        public override int EnvironmentInputCount => _evaluator.EnvironmentInputCount;
        public override int EnvironmentOutputCount => _evaluator.EnvironmentOutputCount;
        public override int ControllerInputCount => _evaluator.ControllerInputCount;
        public override int ControllerOutputCount => _evaluator.ControllerOutputCount;

        private int _substrateLayerConnectionCount = 2;

        public override IGenomeDecoder<NeatGenome, IBlackBox> CreateGenomeDecoder()
        {
            var inputLayerCount = ControllerOutputCount + EnvironmentOutputCount;
            var InputLayer = new SubstrateNodeSet(inputLayerCount, SubstrateNodeSet.LayerType.Input);
            var outputLayerCount = ControllerInputCount + EnvironmentInputCount;
            var OutputLayer = new SubstrateNodeSet(outputLayerCount, SubstrateNodeSet.LayerType.Output);
            const int hiddenLayerX = 3;
            const int hiddenLayerY = 3;
            const int hiddenLayerCount = hiddenLayerX*hiddenLayerY;
            var HiddenLayer = new SubstrateNodeSet(hiddenLayerCount, SubstrateNodeSet.LayerType.Hidden);

            uint inputId = 1, outputId = inputId + (uint)inputLayerCount, hiddenId = outputId + (uint)outputLayerCount;

            for (int i = 0; i < inputLayerCount; ++i)
            {
                InputLayer.NodeList.Add(new SubstrateNode(inputId++, new [] {(double)i/inputLayerCount, 0.0, -1.0}));
            }
            for(int i = 0; i < outputLayerCount; ++i)
            {
                OutputLayer.NodeList.Add(new SubstrateNode(outputId++, new [] {(double)i/inputLayerCount, 0.0, 1.0}));
            }
            for (int x = 0; x < hiddenLayerX; x++)
            {
                for (int y = 0; y < hiddenLayerY; y++)
                {
                    HiddenLayer.NodeList.Add(new SubstrateNode(hiddenId++, new [] {(double)x/hiddenLayerX, (double)y/hiddenLayerY, 0.0}));
                }
            }

            var nodeSetList = new List<SubstrateNodeSet>()
            {
                InputLayer,
                OutputLayer,
                HiddenLayer
            };

            var nodeSetMappingList = new List<NodeSetMapping>
            {
                NodeSetMapping.Create(0, 2, (double?) null),
                NodeSetMapping.Create(2, 1, (double?) null)
            };

            //TODO Is the activation function library provided here used for the CPPN or the resulting ANN?
            var substrate = new Substrate(nodeSetList, DefaultActivationFunctionLibrary.CreateLibraryCppn(), 0, 0.2, 5, nodeSetMappingList);

            var genomeDecoder = new HyperNeatDecoder(substrate, _activationScheme, _activationScheme, false);

            return genomeDecoder;
        }

        public override IGenomeFactory<NeatGenome> CreateGenomeFactory()
        {
            return new NeatGenomeFactory(6, 2, DefaultActivationFunctionLibrary.CreateLibraryCppn(), _neatGenomeParams);
        }
    }

}
