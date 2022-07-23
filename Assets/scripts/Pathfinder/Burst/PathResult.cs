using System;
using Unity.Collections;
using Unity.Jobs;

namespace Pathfinder.Burst
{
    /// <summary>
    /// Path result
    /// </summary>
    public readonly struct PathResult : IDisposable
    {
        public readonly PathRequest Request;
        public readonly NativeList<PathNode> Result;

        public bool IsComplete => _pathfindingJob.IsCompleted;
        
        public PathResult(PathRequest request, NativeList<PathNode> result, JobHandle pathfindingJob)
        {
            Request = request;
            Result = result;
            
            _pathfindingJob = pathfindingJob;
        }

        public void Dispose()
        {
            if (!Result.IsCreated)
            {
                return;
            }
            
            Result.Dispose();
        }

        private readonly JobHandle _pathfindingJob;
    }
}