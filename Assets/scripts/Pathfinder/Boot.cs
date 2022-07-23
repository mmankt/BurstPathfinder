using System.Collections.Generic;
using System.Diagnostics;
using Pathfinder.Burst;
using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class Boot : MonoBehaviour
{
    [SerializeField] private Transform _start;
    [SerializeField] private Transform _end;
    
    private readonly Stopwatch _stopwatch = new ();

    private BurstPathfinder _pathfinder;

    private void Start()
    {
        BuildTestGraph(1000, 1000);
    }

    private void Update()
    {
        var startPosition = _start.position;
        var endPosition = _end.position;

        _stopwatch.Reset();
        _stopwatch.Start();
        
        var path = _pathfinder.FindPathImmediate(new float2(startPosition.x, startPosition.y), new float2(endPosition.x, endPosition.y));
        _stopwatch.Stop();
        var timeSpent = _stopwatch.Elapsed.TotalMilliseconds;
        
        Debug.LogError($"path took {timeSpent} ms");
        
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

        path.Dispose();
    }

    private void OnDestroy()
    {
        _pathfinder.Dispose();
    }

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
    }
}
