using System;
using Unity.Collections;
using Unity.Jobs;

namespace Pathfinder.Burst
{
    /// <summary>
    /// Path result
    /// </summary>
    public struct PathResult : IDisposable
    {
        public readonly PathRequest Request;
        public readonly NativeList<PathNode> Path;

        public bool IsComplete => _pathfindingJob.IsCompleted;
        
        public PathResult(PathRequest request, NativeList<PathNode> path, JobHandle pathfindingJob)
        {
            Request = request;
            Path = path;
            
            _pathfindingJob = pathfindingJob;
        }

        public void Dispose()
        {
            if (!Path.IsCreated)
            {
                return;
            }
            
            Path.Dispose();
        }
        
        public void Complete() => _pathfindingJob.Complete();

        private readonly JobHandle _pathfindingJob;
    }
}