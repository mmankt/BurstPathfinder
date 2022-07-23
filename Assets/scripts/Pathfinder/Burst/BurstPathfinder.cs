using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Pathfinder.Burst
{
    public sealed class BurstPathfinder : IDisposable
    {
        public BurstPathfinder(IReadOnlyList<PathNodeInfo> nodes)
        {
            RebuildGraph(nodes);
        }

        public void Dispose()
        {
            if (_nodes.IsCreated)
            {
                _nodes.Dispose();
            }

            if (_nodeNeighbours.IsCreated)
            {
                _nodeNeighbours.Dispose();
            }
        }

        public NativeList<PathNode> FindPathImmediate(float2 from, float2 to)
        {
            if (!_nodes.IsCreated || _nodes.Length == 0)
            {
                return default;
            }
            
            var job = CratePathfindingJob(from, to);
            
            job.Schedule().Complete();

            return job.OutPath;
        }
        
        public void RebuildGraph([NotNull] IReadOnlyList<PathNodeInfo> nodes)
        {
            Dispose();

            if (nodes == null)
            {
                throw new ArgumentNullException(nameof(nodes));
            }

            if (nodes.Count == 0)
            {
                Debug.LogError("Empty node list provided");
                return;
            }
            
            _nodes = new NativeArray<PathNode>(nodes.Count, Allocator.Persistent);
            _nodeNeighbours = new NativeParallelMultiHashMap<int, int>(nodes.Count, Allocator.Persistent);

            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                _nodes[i] = node.Node;

                for (var j = 0; j < node.Neighbours.Count; j++)
                {
                    _nodeNeighbours.Add(node.Node.Index, node.Neighbours[j].Index);
                }
            }
            
            Debug.LogError($"Graph created with {nodes.Count} nodes.");
        }

        private NativeArray<PathNode> _nodes;
        private NativeParallelMultiHashMap<int, int> _nodeNeighbours;

        private PathfindingJob CratePathfindingJob(float2 from, float2 to)
            => new()
            {
                From = from,
                To = to,
                InputNodes = _nodes,
                NodeNeighbours = _nodeNeighbours,
                OutPath = new NativeList<PathNode>(256, Allocator.TempJob),
            };
    }
}