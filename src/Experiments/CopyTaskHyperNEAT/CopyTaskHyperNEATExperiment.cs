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

        public new IGenomeDecoder<NeatGenome, IBlackBox> CreateGenomeDecoder()
        {
            var inputLayerCount = ControllerInputCount + EnvironmentOutputCount;
            var InputLayer = new SubstrateNodeSet(inputLayerCount);
            var outputLayerCount = ControllerOutputCount + EnvironmentInputCount;
            var OutputLayer = new SubstrateNodeSet(outputLayerCount);
            const int hiddenLayerX = 3;
            const int hiddenLayerY = 3;
            const int hiddenLayerCount = hiddenLayerX*hiddenLayerY;
            var HiddenLayer = new SubstrateNodeSet(hiddenLayerCount);

            uint inputId = 1, outputId = inputId + (uint)outputLayerCount, hiddenId = outputId + (uint)hiddenLayerCount;

            for (int i = 0; i < inputLayerCount; ++i)
            {
                InputLayer.NodeList.Add(new SubstrateNode(inputId++, new [] {(double)i/inputLayerCount}));
            }
            for(int i = 0; i < outputLayerCount; ++i)
            {
                OutputLayer.NodeList.Add(new SubstrateNode(outputId++, new [] {(double)i/inputLayerCount}));
            }
            for (int x = 0; x < hiddenLayerX; x++)
            {
                for (int y = 0; y < hiddenLayerY; y++)
                {
                    HiddenLayer.NodeList.Add(new SubstrateNode(hiddenId++, new [] {(double)x/hiddenLayerX, (double)y/hiddenLayerY}));
                }
            }

            var nodeSetList = new List<SubstrateNodeSet>()
            {
                InputLayer,
                HiddenLayer,
                OutputLayer
            };

            var nodeSetMappingList = new List<NodeSetMapping>
            {
                NodeSetMapping.Create(0, 1, (double?) null),
                NodeSetMapping.Create(1, 2, (double?) null)
            };

            var substrate = new Substrate(nodeSetList, DefaultActivationFunctionLibrary.CreateLibraryCppn(), 0, 0.2, 5, nodeSetMappingList);

            var genomeDecoder = new HyperNeatDecoder(substrate, _activationScheme, _activationScheme, false);

            return genomeDecoder;
        }
    }
}
