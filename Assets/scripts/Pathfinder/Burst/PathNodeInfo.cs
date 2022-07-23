using System;
using System.Collections.Generic;

namespace Pathfinder.Burst
{
    /// <summary>
    /// Node setup info
    /// </summary>
    public sealed class PathNodeInfo : IDisposable
    {
        public readonly PathNode Node;
        
        public IReadOnlyList<PathNode> Neighbours => _neighbours;

        public PathNodeInfo(PathNode node)
        {
            Node = node;
        }
        
        public void Dispose() => _neighbours.Clear();

        public void AddNeighbour(PathNode neighbour)
            => _neighbours.Add(neighbour);

        private readonly List<PathNode> _neighbours = new();
    }
}