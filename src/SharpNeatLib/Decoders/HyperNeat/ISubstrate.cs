using System.Collections.Generic;
using SharpNeat.Network;
using SharpNeat.Phenomes;

namespace SharpNeat.Decoders.HyperNeat
{
    public interface ISubstrate
    {
        /// <summary>
        /// Gets the list of substrate node sets. By convention the first nodeset describes the inputs nodes and the
        /// first node of that set describes the bias node. The last nodeset describes the output nodes.
        /// </summary>
        List<SubstrateNodeSet> NodeSetList { get; }

        int M { get;}
        int N { get; }
        int Dimensionality { get; }

        /// <summary>
        /// Create a network definition by querying the provided IBlackBox (typically a CPPN) with the 
        /// substrate connection endpoints.
        /// </summary>
        /// <param name="blackbox">The HyperNEAT CPPN that defines the strength of connections between nodes on the substrate.</param>
        /// <param name="lengthCppnInput">Optionally we provide a connection length input to the CPPN.</param>
        INetworkDefinition CreateNetworkDefinition(IBlackBox blackbox, bool lengthCppnInput);
    }
}