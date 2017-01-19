﻿using ENTM.Base;

namespace ENTM.Experiments.Xor
{
    public class XorExperiment : BaseExperiment<XorEvaluator, XorEnvironment, DefaultController>
    {
        public override int EnvironmentInputCount => _evaluator.EnvironmentInputCount;
        public override int EnvironmentOutputCount => _evaluator.EnvironmentOutputCount;
        public override int ControllerInputCount => _evaluator.ControllerInputCount;
        public override int ControllerOutputCount => _evaluator.ControllerOutputCount;
    }
}
