using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

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

        

        public override void Initialize(string name, XmlElement xmlConfig)
        {
            base.Initialize(name, xmlConfig);
            
            
            //TODO Initialize substrate from XML
        }
    }
}
