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
    public class CopyTaskHyperNEATExperiment : BaserHyperNEATExperiment<CopyTaskEvaluator, CopyTaskEnvironment, TuringController>
    {
        
    }

}
