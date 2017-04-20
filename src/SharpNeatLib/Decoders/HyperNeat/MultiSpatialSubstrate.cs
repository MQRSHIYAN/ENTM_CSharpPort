/* ***************************************************************************
 * This file is part of SharpNEAT - Evolution of Neural Networks.
 * 
 * Copyright 2004-2006, 2009-2010 Colin Green (sharpneat@gmail.com)
 *
 * SharpNEAT is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * SharpNEAT is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with SharpNEAT.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpNeat.Network;
using SharpNeat.Phenomes;

namespace SharpNeat.Decoders.HyperNeat
{
    public class MultiSpatialSubstrate : ISubstrate
    {
        /// <summary>
        /// The maximum number of substrate conenctions that we cache when using _nodeSetMappingList. If the number of 
        /// connections is less then this then we cache the susbstrate connections to avoid having to invoke the mapping 
        /// functions when creating/growing a network fromt the substrate.
        /// </summary>
        const int ConnectionCountCacheThreshold = 50000;
        /// <summary>
        /// The SubstrateNodeSets which make up the input layers, output layers and hidden layers respectively.
        /// </summary>
        readonly List<SubstrateNodeSet> _inputLayers;
        readonly List<SubstrateNodeSet> _outputLayers;
        readonly List<SubstrateNodeSet> _hiddenLayers;

        /// <summary>
        /// The activation function library allocated to the networks that are 'grown' from the substrate.
        /// _activationFnId refers to a function in this library.
        /// </summary>
        readonly IActivationFunctionLibrary _activationFnLibrary;
        /// <summary>
        /// The activation function ID that is uniformly allocated to all nodes in the netorks that are 'grown' 
        /// from the substrate.
        /// </summary>
        readonly int _activationFnId;
        /// <summary>
        /// A list of mapping functions that provide a means of obtaining a list of substrate connections 
        /// from the _nodeSetList. 
        /// </summary>
        readonly List<NodeSetMapping> _nodeSetMappingList;
        /// <summary>
        /// Pre-built set of substrate connections.
        /// </summary>
        readonly List<SubstrateConnection>[] _connectionList;
        /// <summary>
        /// A hint to the method creating networks from substrate - approximate number of connections that can be 
        /// expected to be grown by the substrate and it's mapping functions.
        /// </summary>
        readonly int _connectionCountHint;
        /// <summary>
        /// The weight threshold below which substrate connections are not created in grown networks.
        /// </summary>
        readonly double _weightThreshold;
        /// <summary>
        /// Defines the weight range of grown connections (+-maxWeight).
        /// </summary>
        readonly double _maxWeight;
        /// <summary>
        /// Precalculated value for rescaling grown conenctions to the required weight range as described by _maxWeight.
        /// </summary>
        readonly double _weightRescalingCoeff;
        /// <summary>
        /// Pre-built node list for creating new concrete network instances. This can be prebuilt because
        /// the set of nodes remains the same for each network instantiation, only the connections differ between
        /// instantiations.
        /// </summary>
        readonly NodeList _netNodeList;
        /// <summary>
        /// Dimensionality of the substrate. The number of coordinate values in a node position; typically 2D or 3D.
        /// </summary>
        public int Dimensionality { get; }

        private List<SubstrateNodeSet> _nodesetlist;

        /// <summary>
        /// All layers in the substrate together
        /// </summary>
        public List<SubstrateNodeSet> NodeSetList => _nodesetlist ??
                                                     (_nodesetlist = _inputLayers.Concat(_outputLayers).Concat(_hiddenLayers).ToList());

        public int M => _hiddenLayers.Count + _outputLayers.Count;
        public int N => _connectionList.Length;
        public bool Leo { get; set; }
        
        /// <summary>
        /// Construct a substrate with the provided node sets and a predetermined set of connections. 
        /// </summary>
        /// <param name="nodeSetList">Substrate nodes, represented as distinct sets of nodes. By convention the first and second
        /// sets in the list represent the input and output noes respectively. All other sets represent hidden nodes.</param>
        /// <param name="activationFnLibrary">The activation function library allocated to the networks that are 'grown' from the substrate.</param>
        /// <param name="activationFnId">The ID of an activation function function in activationFnLibrary. This is the activation function 
        /// ID assigned to all nodes in networks that are 'grown' from the substrate. </param>
        /// <param name="weightThreshold">The weight threshold below which substrate connections are not created in grown networks.</param>
        /// <param name="maxWeight">Defines the weight range of grown connections (+-maxWeight).</param>
        /// <param name="connectionList">A predetermined list of substrate connections.</param>
        public MultiSpatialSubstrate(List<SubstrateNodeSet> nodeSetList,
                         IActivationFunctionLibrary activationFnLibrary, int activationFnId,
                         double weightThreshold, double maxWeight,
                         List<SubstrateConnection> connectionList)
        {
            var inputLayers = nodeSetList.Where(x => x.Type == SubstrateNodeSet.LayerType.Input).ToList();
            var outputLayers = nodeSetList.Where(x => x.Type == SubstrateNodeSet.LayerType.Output).ToList();
            var hiddenLayers = nodeSetList.Where(x => x.Type == SubstrateNodeSet.LayerType.Hidden).ToList();
            VaildateSubstrateNodes(inputLayers, outputLayers, hiddenLayers);

            _inputLayers = inputLayers;
            _outputLayers = outputLayers;
            _hiddenLayers = hiddenLayers;
            Dimensionality = _inputLayers[0].NodeList[0]._position.GetUpperBound(0) + 1;

            _activationFnLibrary = activationFnLibrary;
            _activationFnId = activationFnId;

            _weightThreshold = weightThreshold;
            _maxWeight = maxWeight;
            _weightRescalingCoeff = _maxWeight / (1.0 - _weightThreshold);

            // Set total connection count hint value (includes additional connections to a bias node).
            _connectionList = CreateConnectionArray(connectionList);
            _connectionCountHint = connectionList.Count + CalcBiasConnectionCountHint();

            // Pre-create the network definition node list. This is re-used each time a network is created from the substrate.
            _netNodeList = CreateNetworkNodeList();
        }

        /// <summary>
        /// Constructs with the provided substrate nodesets and mappings that describe how the nodesets are to be connected up.
        /// </summary>
        /// <param name="nodeSetList">Substrate nodes, represented as distinct sets of nodes. By convention the first and second
        /// sets in the list represent the input and output noes respectively. All other sets represent hidden nodes.</param>
        /// <param name="activationFnLibrary">The activation function library allocated to the networks that are 'grown' from the substrate.</param>
        /// <param name="activationFnId">The ID of an activation function function in activationFnLibrary. This is the activation function 
        /// ID assigned to all nodes in networks that are 'grown' from the substrate. </param>
        /// <param name="weightThreshold">The weight threshold below which substrate connections are not created in grown networks.</param>
        /// <param name="maxWeight">Defines the weight range of grown connections (+-maxWeight).</param>/// 
        /// <param name="nodeSetMappingList">A list of mappings between node sets that defines what connections to create between substrate nodes.</param>
        public MultiSpatialSubstrate(List<SubstrateNodeSet> nodeSetList,
                         IActivationFunctionLibrary activationFnLibrary, int activationFnId,
                         double weightThreshold, double maxWeight,
                         List<NodeSetMapping> nodeSetMappingList)
        {
            var inputLayers = nodeSetList.Where(x => x.Type == SubstrateNodeSet.LayerType.Input).ToList();
            var outputLayers = nodeSetList.Where(x => x.Type == SubstrateNodeSet.LayerType.Output).ToList();
            var hiddenLayers = nodeSetList.Where(x => x.Type == SubstrateNodeSet.LayerType.Hidden).ToList();
            VaildateSubstrateNodes(inputLayers, outputLayers, hiddenLayers);

            _inputLayers = inputLayers;
            _outputLayers = outputLayers;
            _hiddenLayers = hiddenLayers;
            Dimensionality = _inputLayers[0].NodeList[0]._position.GetUpperBound(0) + 1;

            _activationFnLibrary = activationFnLibrary;
            _activationFnId = activationFnId;

            _weightThreshold = weightThreshold;
            _maxWeight = maxWeight;
            _weightRescalingCoeff = _maxWeight / (1.0 - _weightThreshold);

            _nodeSetMappingList = nodeSetMappingList;

            // Get an estimate for the number of connections defined by mappings.
            _connectionList = CreateConnectionArray(nodeSetMappingList);
            var sum = 0;
            foreach (List<SubstrateConnection> t in _connectionList)
            {
                sum += t.Count;
            }
            _connectionCountHint = sum + CalcBiasConnectionCountHint();
            // Pre-create the network definition node list. This is re-used each time a network is created from the substrate.
            _netNodeList = CreateNetworkNodeList();

        }

        /// <summary>
        /// Create a network definition by querying the provided IBlackBox (typically a CPPN) with the 
        /// substrate connection endpoints.
        /// </summary>
        /// <param name="blackbox">The HyperNEAT CPPN that defines the strength of connections between nodes on the substrate.</param>
        /// <param name="lengthCppnInput">Optionally we provide a connection length input to the CPPN.</param>
        public INetworkDefinition CreateNetworkDefinition(IBlackBox blackbox, bool lengthCppnInput)
        {

            // Iterate over substrate connections. Determine each connection's weight and create a list
            // of network definition connections.
            ISignalArray inputSignalArr = blackbox.InputSignalArray;
            ISignalArray outputSignalArr = blackbox.OutputSignalArray;
            ConnectionList networkConnList = new ConnectionList(_connectionCountHint);
            int lengthInputIdx = Dimensionality + Dimensionality;

            for (int i = 0; i < N; i++)
            {
                foreach (var substrateConnection in _connectionList[i])
                {
                    for (int j = 0; j < Dimensionality; j++)
                    {
                        inputSignalArr[j] = substrateConnection._srcNode._position[j];
                        inputSignalArr[j + Dimensionality] = substrateConnection._tgtNode._position[j];
                    }
                    // Optional connection length input.
                    if (lengthCppnInput)
                    {
                        inputSignalArr[lengthInputIdx] = CalculateConnectionLength(substrateConnection._srcNode._position, substrateConnection._tgtNode._position);
                    }
                    blackbox.ResetState();
                    blackbox.Activate();
                    double weight = outputSignalArr[i];

                    //if LEO is toggled query for expression
                    double expressionWeight = -0.1;
                    if (Leo)
                    {
                        expressionWeight = outputSignalArr[i + M + N];
                    }
                    // Skip connections with a weight magnitude less than _weightThreshold.
                    double weightAbs = Math.Abs(weight);
                    if (!Leo && weightAbs > _weightThreshold || Leo && expressionWeight >= 0.0)
                    {
                        // For weights over the threshold we re-scale into the range [-_maxWeight,_maxWeight],
                        // assuming IBlackBox outputs are in the range [-1,1].
                        weight = (weightAbs - _weightThreshold) * _weightRescalingCoeff * Math.Sign(weight);

                        // Create network definition connection and add to list.
                        networkConnList.Add(new NetworkConnection(substrateConnection._srcNode._id,
                                                                  substrateConnection._tgtNode._id, weight));
                    }
                }
            }
            var biasOutputIdx = N;
            foreach (var nodeSet in _outputLayers.Concat(_hiddenLayers))
            {
                foreach (var node in nodeSet.NodeList)
                {
                    // Assign the node's position coords to the blackbox inputs. The CPPN inputs for source node coords are set to zero when obtaining bias values.
                    for (int j = 0; j < Dimensionality; j++)
                    {
                        inputSignalArr[j] = 0.0;
                        inputSignalArr[j + Dimensionality] = node._position[j];
                    }

                    // Optional connection length input.
                    if (lengthCppnInput)
                    {
                        inputSignalArr[lengthInputIdx] = CalculateConnectionLength(node._position);
                    }

                    // Reset blackbox state and activate it.
                    blackbox.ResetState();
                    blackbox.Activate();

                    // Read bias connection weight from output 1.
                    double weight = outputSignalArr[biasOutputIdx];
                    // Skip connections with a weight magnitude less than _weightThreshold.
                    double weightAbs = Math.Abs(weight);
                    if (weightAbs > _weightThreshold)
                    {
                        // For weights over the threshold we re-scale into the range [-_maxWeight,_maxWeight],
                        // assuming IBlackBox outputs are in the range [-1,1].
                        weight = (weightAbs - _weightThreshold) * _weightRescalingCoeff * Math.Sign(weight);

                        // Create network definition connection and add to list. Bias node is always ID 0.
                        networkConnList.Add(new NetworkConnection(0, node._id, weight));
                    }
                }
                biasOutputIdx++;
            }
            
            // Check for no connections.
            // If no connections were generated then there is no point in further evaulating the network.
            // However, null is a valid response when decoding genomes to phenomes, therefore we do that here.
            if (networkConnList.Count == 0)
            {
                return null;
            }

            // Construct and return a network definition.
            NetworkDefinition networkDef = new NetworkDefinition(_inputLayers.Sum(x => x.NodeList.Count), _outputLayers.Sum(x => x.NodeList.Count),
                                                                 _activationFnLibrary, _netNodeList, networkConnList);

            // Check that the definition is valid and return it.
            Debug.Assert(networkDef.PerformIntegrityCheck());
            return networkDef;
        }

        private void VaildateSubstrateNodes(List<SubstrateNodeSet> inputLayer, List<SubstrateNodeSet> outputLayer, List<SubstrateNodeSet> hiddenLayer)
        {
            // Baseline validation tests. There should be at least two nodesets (input and output sets), and each of those must have at least one node.
            if (inputLayer.Count == 0 || outputLayer.Count == 0)
            {
                throw new ArgumentException("Substrate requires a minimum of two NodeSets - one each for input and outut nodes.");
            }

            // Input nodes.
            if (inputLayer.Any(x => x.NodeList.Count == 0))
            {
                throw new ArgumentException("Substrate input nodeset must have at least one node.");
            }

            if (outputLayer.Any(x => x.NodeList.Count == 0))
            {
                throw new ArgumentException("Substrate output nodeset must have at least one node.");
            }

            // Check for duplicate IDs or ID zero (reserved for bias node).
            Dictionary<uint, object> idDict = new Dictionary<uint, object>();
            foreach (SubstrateNodeSet nodeSet in inputLayer.Concat(outputLayer).Concat(hiddenLayer))
            {
                foreach (SubstrateNode node in nodeSet.NodeList)
                {
                    if (0u == node._id)
                    {
                        throw new ArgumentException("Substrate node with invalid ID of 0 (reserved for bias node).");
                    }
                    if (idDict.ContainsKey(node._id))
                    {
                        throw new ArgumentException(string.Format("Substrate node with duplicate ID of [{0}]", node._id));
                    }
                    idDict.Add(node._id, null);
                }
            }

            // Check ID ordering.
            // Input node IDs should be contiguous and ordered sequentially after the bais node with ID 0.
            // Output node IDs should be contiguous and ordered sequentially after the input nodes.
            int expectedId = 1;
            foreach (var substrateNodeSet in inputLayer.Concat(outputLayer))
            {
                var count = substrateNodeSet.NodeList.Count;
                for (int i = 0; i < count; i++, expectedId++)
                {
                    if (substrateNodeSet.NodeList[i]._id != expectedId)
                    {
                        throw new ArgumentException(string.Format("Substrate node with unexpected ID of [{0}]. Ids should be contguous and starting from 1.", substrateNodeSet.NodeList[i]._id));
                    }
                }
            }

            // Hidden node IDs don't have to be contiguous but must have IDs greater than all of the input and output IDs.
            foreach (var substrateNodeSet in hiddenLayer)
            {
                foreach (var substrateNode in substrateNodeSet.NodeList)
                {
                    if (substrateNode._id < expectedId)
                    {
                        throw new ArgumentException(string.Format("Substrate hidden node with unexpected ID of [{0}] (must be greater than the last output node ID [{1}].",
                                                                  substrateNode._id, expectedId - 1));
                    }
                }
            }
        }

        private List<SubstrateConnection>[] CreateConnectionArray(List<SubstrateConnection> connectionList)
        {
            var map = new Dictionary<Tuple<int, int>, List<SubstrateConnection>>();
            foreach (var con in connectionList)
            {
                var idx = 0;
                int srcLayer = -1;
                int tgtLayer = -1;
                for (int i = 0; i < _inputLayers.Count; i++, idx++)
                {
                    if (_inputLayers[i].NodeList.Contains(con._srcNode))
                    {
                        srcLayer = idx;
                    }
                    if (_inputLayers[i].NodeList.Contains(con._tgtNode))
                    {
                        tgtLayer = idx;
                    }
                }
                for (int i = 0; i < _outputLayers.Count; i++, idx++)
                {
                    if (_outputLayers[i].NodeList.Contains(con._srcNode))
                    {
                        srcLayer = idx;
                    }
                    if (_outputLayers[i].NodeList.Contains(con._tgtNode))
                    {
                        tgtLayer = idx;
                    }
                }
                for (int i = 0; i < _hiddenLayers.Count; i++, idx++)
                {
                    if (_hiddenLayers[i].NodeList.Contains(con._srcNode))
                    {
                        srcLayer = idx;
                    }
                    if (_hiddenLayers[i].NodeList.Contains(con._tgtNode))
                    {
                        tgtLayer = idx;
                    }
                }
                Debug.Assert(srcLayer > -1 && tgtLayer > -1);

                var key = new Tuple<int, int>(srcLayer, tgtLayer);
                if (!map.ContainsKey(key))
                {
                    map[key] = new List<SubstrateConnection>();
                }
                map[key].Add(con);
            }
            var result = new List<SubstrateConnection>[map.Keys.Count];
            int j = 0;
            foreach (var value in map.Values)
            {
                result[j] = value;
                j++;
            }
            return result;
        }
        private List<SubstrateConnection>[] CreateConnectionArray(List<NodeSetMapping> nodeSetMappingList)
        {
            var result = new List<SubstrateConnection>[nodeSetMappingList.Count];
            var i = 0;
            foreach (var nodeSetMapping in nodeSetMappingList)
            {
                result[i] = nodeSetMapping.GenerateConnections(NodeSetList).ToList();
                i++;
            }
            return result;
        }
        /// <summary>
        /// Calculate the maximum number of possible bias connections. Input nodes don't have a bias therefore this value
        /// is the number of hidden and output nodes.
        /// </summary>
        private int CalcBiasConnectionCountHint()
        {
            // Count nodes in all nodesets except for the first (input) nodeset.
            int total = 0;
            foreach (var source in _outputLayers.Concat(_hiddenLayers))
            {
                total += source.NodeList.Count;
            }
            return total;
        }

        // <summary>
        /// Pre-build the network node list used for constructing new networks 'grown' on the substrate.
        /// This can be prebuilt because the set of nodes remains the same for each network instantiation,
        /// only the connections differ between instantiations.
        /// </summary>
        private NodeList CreateNetworkNodeList()
        {
            // Count the total number of nodes.
            int nodeCount = 0;
            foreach (SubstrateNodeSet set in NodeSetList)
            {
                nodeCount += set.NodeList.Count;
            }
            // Count the additional bias node (not explicitly defined on the substrate).
            nodeCount++;

            // Allocate storage for the nodes.
            NodeList nodeList = new NodeList(nodeCount);

            // Create bias node.
            // Note. The nodes are created in the order of inputs, outputs and then hidden. This is the order required when constructing
            // instances of the NeatGenome and NetworkDefinition classes. The requirement comes about through internal implementation of 
            // those classes - see comments on those classes for more info.
            nodeList.Add(new NetworkNode(0u, NodeType.Bias, _activationFnId));

            // Create input nodes. By convention the first nodeset describes the input nodes (not including the bias).
            foreach (var substrateNodeSet in _inputLayers)
            {
                foreach (var node in substrateNodeSet.NodeList)
                {
                    nodeList.Add(new NetworkNode(node._id, NodeType.Input, _activationFnId));
                }
            }
            // Create output nodes. By convention the second nodeset describes the output nodes.
            foreach (var substrateNodeSet in _outputLayers)
            {
                foreach (var node in substrateNodeSet.NodeList)
                {
                    nodeList.Add(new NetworkNode(node._id, NodeType.Output, _activationFnId));
                }
            }
            //LINQ....
            //nodeList.AddRange(_outputLayers.SelectMany(x => x.NodeList).Select(x => new NetworkNode(x._id, NodeType.Output, _activationFnId)));

            // Create hidden nodes (if any). All nodesets after the input and output nodesets define hidden nodes.
            foreach (var substrateNodeSet in _hiddenLayers)
            {
                foreach (var node in substrateNodeSet.NodeList)
                {
                    nodeList.Add(new NetworkNode(node._id, NodeType.Hidden, _activationFnId));
                }
            }

            return nodeList;
        }

        /// <summary>
        /// Calculates the euclidean distance between two points in N dimensional space.
        /// </summary>
        private double CalculateConnectionLength(double[] a, double[] b)
        {
            double acc = 0.0;
            for (int i = 0; i < a.Length; i++)
            {
                acc += (a[i] - b[i]) * (a[i] - b[i]);
            }
            return Math.Sqrt(acc);
        }

        /// <summary>
        /// Calculates the euclidean distance between a point and the origin.
        /// </summary>
        private double CalculateConnectionLength(double[] a)
        {
            double acc = 0.0;
            for (int i = 0; i < a.Length; i++)
            {
                acc += a[i] * a[i];
            }
            return Math.Sqrt(acc);
        }
    }
}
