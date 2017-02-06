using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using ENTM.Utility;
using SharpNeat.Decoders.HyperNeat;
using SharpNeat.Domains;

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

        public override void Initialize(string name, XmlElement xmlConfig)
        {
            base.Initialize(name, xmlConfig);
            _substrate = ExperimentUtils.ReadSubstrateFromXml(xmlConfig.GetElementsByTagName("Substrate").)
            
            //TODO Initialize substrate from XML
        }
    }
}
