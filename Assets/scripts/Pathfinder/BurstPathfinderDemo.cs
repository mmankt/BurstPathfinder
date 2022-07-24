using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Pathfinder.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Pathfinder
{
    /// <summary>
    /// Demo usage of the burst pathfinder 
    /// </summary>
    public sealed class BurstPathfinderDemo : MonoBehaviour
    {
        [SerializeField] private Transform _start;
        [SerializeField] private Transform _end;
    
        private readonly Stopwatch _stopwatch = new ();

        private BurstPathfinder _pathfinder;
    
        private static void DrawDebugPath(in NativeList<PathNode> path)
        {
            if (!path.IsCreated)
            {
                return;
            }

            for (var i = 1; i < path.Length; i++)
            {
                var currNode = path[i];
                var prevNode = path[i - 1];

                Debug.DrawLine(prevNode.Position.xyy, currNode.Position.xyx, Color.green);
            }
        }
    
        private void Start() => BuildTestGraph(100, 100);

        private void Update() => UpdateInternal();

        private void OnDestroy() => _pathfinder.Dispose();

        private void BuildTestGraph(int x, int y)
        {
            var size = new Vector2Int(x, y);
            var nodes = new List<PathNodeInfo>();
            var nodeIndex = 0;

            for (var i = 0; i < size.y; i++)//y
            {
                for (var j = 0; j < size.x; j++)//x
                {
                    var node = new PathNode(nodeIndex, new float2((float)i / size.x * 10f, (float)j/size.y * 10f));
                    var nodeInfo = new PathNodeInfo(node);
                    nodes.Add(nodeInfo);
                
                    nodeIndex++;
                }
            }

            for (var i = 0; i < size.y; i++)//y
            {
                for (var j = 0; j < size.x; j++)//x
                {
                    var nodeInfo = nodes[i * size.x + j];
                
                    //left
                    if (j != size.x - 1)
                    {
                        nodeInfo.AddNeighbour((nodes[i * size.x + j + 1].Node));
                        nodes[i * size.x + j + 1].AddNeighbour(nodeInfo.Node);
                    }
                
                    //upper
                    if (i != size.y - 1)
                    {
                        nodeInfo.AddNeighbour(nodes[(i + 1) * size.x + j].Node);
                        nodes[(i + 1) * size.x + j].AddNeighbour(nodeInfo.Node);
                    }
                
                    //diagonal
                    if (i != size.y - 1 && j != size.x - 1)
                    {
                        nodeInfo.AddNeighbour(nodes[(i + 1) * size.x + j+1].Node);
                        nodes[(i + 1) * size.x + j+1].AddNeighbour(nodeInfo.Node);
                    }

                    //counter diagonal
                    if (i != size.y - 1 && j != 0)
                    {
                        nodeInfo.AddNeighbour(nodes[(i + 1) * size.x + j - 1].Node);
                        nodes[(i + 1) * size.x + j - 1].AddNeighbour(nodeInfo.Node);
                    }
                }
            }
        
            _pathfinder = new BurstPathfinder(nodes);

            _stopwatch.Start();
        }
    
        private void UpdateInternal()
        {
            var request = CrateTestPathRequest();
            
            //todo async and coroutine waiting for a job to complete is waaay longer than immediate, investigate
            //StartCoroutine(GetPathCoroutine(request));

            //GetPathAsync(request);
            
            //Ryzen 5900x does this in around 0.35 ms for 10000 nodes / 27 ms for 1000000 nodes, burst compilation speeds up c# ~10 times
            GetPathImmediate(request);
        }

        private PathRequest CrateTestPathRequest()
        {
            var startPosition = _start.position;
            var endPosition = _end.position;
            var from = new float2(startPosition.x, startPosition.y);
            var to = new float2(endPosition.x, endPosition.y);
            var request = new PathRequest(from, to);
            
            return request;
        }

        private void GetPathImmediate(PathRequest request)
        {
            var startTime = _stopwatch.Elapsed.TotalMilliseconds;
            
            using var pathResult = _pathfinder.FindPath(request);
            
            pathResult.Complete();
            
            var path = pathResult.Path;
            var endTime = _stopwatch.Elapsed.TotalMilliseconds;
            var timeSpent = endTime - startTime;

            Debug.Log($"Immediate path took {timeSpent} ms");

            DrawDebugPath(path);
        }

        private async void GetPathAsync(PathRequest request)
        {
            var startTime = _stopwatch.Elapsed.TotalMilliseconds;

            using var pathResult = _pathfinder.FindPath(request);
            
            while (!pathResult.IsComplete)
            {
                await Task.Delay(1);
            }

            //Note: looks like despite the job being marked as complete you need to Complete() it as reading from result is throwing errors
            pathResult.Complete();

            var endTime = _stopwatch.Elapsed.TotalMilliseconds;
            var timeSpent = endTime - startTime;

            Debug.Log($"Async path took {timeSpent} ms");

            DrawDebugPath(pathResult.Path);
        }
        
        private IEnumerator GetPathCoroutine(PathRequest request)
        {
            var startTime = _stopwatch.Elapsed.TotalMilliseconds;
            
            using var pathResult = _pathfinder.FindPath(request);
            
            while (!pathResult.IsComplete)
            {
                yield return null;
            }
            
            var endTime = _stopwatch.Elapsed.TotalMilliseconds;
            var timeSpent = endTime - startTime;
            
            //Note: looks like despite the job being marked as complete you need to Complete() it as reading from result is throwing errors
            pathResult.Complete();
         
            Debug.Log($"Coroutine path took {timeSpent} ms");
        
            DrawDebugPath(pathResult.Path);
        }
    }
}
