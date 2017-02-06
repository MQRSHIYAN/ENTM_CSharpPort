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
using SharpNeat.Genomes.Neat;
using SharpNeat.Network;
using SharpNeat.Phenomes;

namespace ENTM.Base
{
    public abstract class BaserHyperNEATExperiment<TEvaluator, TEnvironment, TController> : BaseExperiment<TEvaluator, TEnvironment, TController>
        where TEnvironment : IEnvironment
        where TEvaluator : BaseEvaluator<TEnvironment, TController>, new()
        where TController : IController
    {
        public override int EnvironmentInputCount => _evaluator.EnvironmentInputCount;
        public override int EnvironmentOutputCount => _evaluator.EnvironmentOutputCount;
        public override int ControllerInputCount => _evaluator.ControllerInputCount;
        public override int ControllerOutputCount => _evaluator.ControllerOutputCount;

        protected Substrate _substrate;
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
                ExperimentUtils.ReadSubstrateFromXml(xmlConfig.GetElementsByTagName("Substrate")[0] as XmlElement);
            _cppnActivationScheme = ExperimentUtils.CreateActivationScheme(xmlConfig, "CPPNActivation");
            _cppnInputLength = XmlUtils.TryGetValueAsBool(xmlConfig, "CPPNDistanceInput") ?? false;
        }

        public override IGenomeDecoder<NeatGenome, IBlackBox> CreateGenomeDecoder()
        {
            return new HyperNeatDecoder(_substrate, _cppnActivationScheme, _activationScheme, _cppnInputLength);
        }

        public override IGenomeFactory<NeatGenome> CreateGenomeFactory()
        {
            var numInputs = _cppnInputLength ? _substrate.Dimensionality + 1 : _substrate.Dimensionality;
            //TODO change number of outputs if we implement MSS or otherwise change the CPPN
            return new NeatGenomeFactory(numInputs, 2, DefaultActivationFunctionLibrary.CreateLibraryCppn(), _neatGenomeParams);
        }
    }
}
