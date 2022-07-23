using Unity.Mathematics;

namespace Pathfinder.Burst
{
    /// <summary>
    /// Represents an immutable graph node used in burst job
    /// </summary>
    public readonly struct PathNode
    {
        public readonly int Index;
        public readonly float2 Position;
        public readonly float Cost;
        public readonly bool IsValid;

        public PathNode(int index, float2 position, float cost = 1f)
        {
            Position = position;
            Cost = cost;
            Index = index;
            IsValid = true;
        }
    }
}