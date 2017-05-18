using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using ENTM.Utility;
using SharpNeat.Core;
using SharpNeat.Decoders;
using SharpNeat.Decoders.HyperNeat;
using SharpNeat.Domains;
using SharpNeat.Genomes.HyperNeat;
using SharpNeat.Genomes.Neat;
using SharpNeat.Network;
using SharpNeat.Phenomes;
using SharpNeat.Utility;

namespace ENTM.Base
{
    public abstract class BaseHyperNEATExperiment<TEvaluator, TEnvironment, TController> : BaseExperiment<TEvaluator, TEnvironment, TController>
        where TEnvironment : IEnvironment
        where TEvaluator : BaseEvaluator<TEnvironment, TController>, new()
        where TController : IController
    {
        public override int EnvironmentInputCount => _evaluator.EnvironmentInputCount;
        public override int EnvironmentOutputCount => _evaluator.EnvironmentOutputCount;
        public override int ControllerInputCount => _evaluator.ControllerInputCount;
        public override int ControllerOutputCount => _evaluator.ControllerOutputCount;

        protected ISubstrate _substrate;
        protected NetworkActivationScheme _cppnActivationScheme;
        protected bool _cppnInputLength;

        public override void Initialize(string name, XmlElement xmlConfig)
        {
            base.Initialize(name, xmlConfig);
            var substrateElements = xmlConfig.GetElementsByTagName("Substrate");
            if (substrateElements.Count != 1)
            {
                throw new ArgumentException("Must be only one substrate element in the xml.");
            }            
            _substrate =
                ExperimentUtils.ReadSubstrateFromXml(xmlConfig.GetElementsByTagName("Substrate")[0] as XmlElement, xmlConfig.GetElementsByTagName("SubstrateSettings")[0] as XmlElement);
            _cppnActivationScheme = ExperimentUtils.CreateActivationScheme(xmlConfig, "CPPNActivation");
            _cppnInputLength = XmlUtils.TryGetValueAsBool(xmlConfig, "CPPNDistanceInput") ?? false;
        }

        public override IGenomeDecoder<NeatGenome, IBlackBox> CreateGenomeDecoder()
        {
            return new HyperNeatDecoder(_substrate, _cppnActivationScheme, _activationScheme, _cppnInputLength);
        }

        public override IGenomeFactory<NeatGenome> CreateGenomeFactory()
        {
            return CreateGenomeFactory(new List<NeatGenome>());
            //return new CppnGenomeFactory(numInputs, numOutputs, functionLibrary, _neatGenomeParams);
        }

        public override IGenomeFactory<NeatGenome> CreateGenomeFactory(List<NeatGenome> seedList)
        {
            var numInputs = _cppnInputLength ? _substrate.Dimensionality * 2 + 1 : _substrate.Dimensionality * 2;
            var numOutputs = _substrate.M + _substrate.N + (_substrate.Leo ? _substrate.N : 0);
            var functionLibrary = CreateActivationFunctionLibrary();
            var maxNeuronId = seedList.Count > 0 ? seedList.SelectMany(x => x.NodeList).Max(x => x.Id) : 0;
            var maxConnectionGeneId = seedList.Count > 0 ? seedList.SelectMany(x => (List<ConnectionGene>)x.ConnectionGeneList).Max(x => x.InnovationId) : 0;
            return new CppnGenomeFactory(numInputs, numOutputs, functionLibrary,_neatGenomeParams, new UInt32IdGenerator(maxNeuronId+1), new UInt32IdGenerator(maxConnectionGeneId+1));
        }

        private IActivationFunctionLibrary CreateActivationFunctionLibrary()
        {
            return new DefaultActivationFunctionLibrary(new List<ActivationFunctionInfo>()
            {
                new ActivationFunctionInfo(0, 0.25, BipolarSigmoid.__DefaultInstance),
                new ActivationFunctionInfo(1, 0.25, Linear.__DefaultInstance),
                new ActivationFunctionInfo(2, 0.25, Gaussian.__DefaultInstance),
                new ActivationFunctionInfo(3, 0.25, Sine.__DefaultInstance)
            });
        }
    }
}
