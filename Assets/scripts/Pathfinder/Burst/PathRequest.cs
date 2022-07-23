using Unity.Mathematics;
using UnityEngine;

namespace Pathfinder.Burst
{
    /// <summary>
    /// Path request
    /// </summary>
    public readonly struct PathRequest
    {
        public readonly float2 From;
        public readonly float2 To;
        public readonly byte Priority;
        
        public PathRequest(Vector2 from, Vector2 to, byte priority = 0)
        {
            From = from;
            To = to;
            Priority = priority;
        }
    }
}