using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Pathfinder.Burst
{
    [BurstCompile(FloatPrecision.Low, FloatMode.Fast, OptimizeFor = OptimizeFor.Performance)]
    public struct PathfindingJob : IJob
    {
        public float2 From;
        public float2 To;
        
        [ReadOnly] public NativeArray<PathNode> InputNodes;
        [ReadOnly] public NativeParallelMultiHashMap<int, int> NodeNeighbours;
        
        [WriteOnly] public NativeList<PathNode> OutPath;

        [DeallocateOnJobCompletion] public NativeArray<byte> Cts;

        public void Execute()
        {
            var nodes = new NativeArray<PathNodeCalculation>(InputNodes.Length, Allocator.Temp);
            var indexes = new NativeArray<int>(1024, Allocator.Temp);
            
            for (var i = 0; i < InputNodes.Length; i++)
            {
                nodes[i] = new PathNodeCalculation { Node = InputNodes[i] };
            }

            var start = FindClosest(From);
            var goal = FindClosest(To);

            _queue = new PathNodeQueue();
            _queue.Push(nodes[start.Index], ref nodes, ref indexes);

            while (!_queue.IsEmpty())
            {
                if (Cts[0] == 1)
                {
                    return;
                }
                
                var current = _queue.Pop(ref nodes, ref indexes);
                
                if (current.Node.Index == goal.Index)
                {
                    OutPath.Add(current.Node);

                    while (current.PathParent.IsValid)
                    {
                        OutPath.Add(current.PathParent);
                        current = nodes[current.PathParent.Index];
                    }

                    return;
                }

                current.IsClosed = true;
                nodes[current.Node.Index] = current;

                var neighbours = NodeNeighbours.GetValuesForKey(current.Node.Index);
                while (neighbours.MoveNext())
                {
                    if (Cts[0] == 1)
                    {
                        return;
                    }
                    
                    var neighbour = nodes[neighbours.Current];
                    if (neighbour.IsClosed)
                    {
                        continue;
                    }

                    var neighbourNode = neighbour.Node;
                    var tentativeG = current.G + Heuristic(current.Node.Position, neighbourNode.Position) * (current.Node.Cost + neighbourNode.Cost) / 2;

                    if (neighbour.IsQueued && (tentativeG >= neighbour.G))
                    {
                        continue;
                    }
                    
                    neighbour.PathParent = current.Node;
                    neighbour.G = tentativeG;
                    neighbour.F = neighbour.G + Heuristic(neighbour.Node.Position, goal.Position);
                     
                    nodes[neighbourNode.Index] = neighbour;
                    
                    if (!neighbour.IsQueued)
                    {
                        _queue.Push(neighbour, ref nodes, ref indexes);
                    }
                }
            }

            nodes.Dispose();
            indexes.Dispose();
        }

        private PathNodeQueue _queue;
        
        private static float Heuristic(float2 p1, float2 p2) => math.abs(p1.x - p2.x) + math.abs(p1.y - p2.y);
        
        private PathNode FindClosest(float2 position)
        {
            var graph = InputNodes;
            var minDist = float.MaxValue;
            PathNode bestPoint = default;

            for (var i = 0; i < graph.Length; i++)
            {
                var distanceSquared = math.distancesq(graph[i].Position, position);
                if ((distanceSquared >= minDist))
                {
                    continue;
                }
                
                minDist = distanceSquared;
                bestPoint = graph[i];
            }

            return bestPoint;
        }

        /// <summary>
        /// Data crucial to pathfinding calculations
        /// </summary>
        private struct PathNodeCalculation
        {
            public PathNode Node;
            public PathNode PathParent;
            public float G;
            public float F;
            public bool IsClosed;
            public bool IsQueued;
        }

        /// <summary>
        /// Implicit binary heap for log(n) lookups.
        /// </summary>
        private struct PathNodeQueue
        {
            //Note: burst doesn't allow native collection fields inside nested structs in IJob structs
            //despite them being passed before scheduling a job so we pass by ref, index array is growing so it has to be a ref
            public void Push(PathNodeCalculation node, ref NativeArray<PathNodeCalculation> nodes,
                ref NativeArray<int> indexes)
            {
                var itemsLength = indexes.Length;    
                if (_itemCount == itemsLength-1)
                {
                    var newItems = new NativeArray<int>(itemsLength * 2, Allocator.Temp);

                    for (var i = 0; i < indexes.Length; i++)
                    {
                        newItems[i] = indexes[i];
                    }

                    indexes.Dispose();
                    indexes = newItems;
                }

                node.IsQueued = true;
                nodes[node.Node.Index] = node;
                indexes[_itemCount++] = node.Node.Index;

                BubbleUp(_itemCount - 1, ref nodes, ref indexes);
            }

            public PathNodeCalculation Pop(ref NativeArray<PathNodeCalculation> nodes, ref NativeArray<int> indexes)
            {
                var first = indexes[0];
                indexes[0] = indexes[--_itemCount];
                
                TrickleDown(0, ref nodes, ref indexes);
                
                var node = nodes[first];
                node.IsQueued = false;
                nodes[first] = node;
                
                return node;
            }
            
            public bool IsEmpty() => _itemCount == 0;

            private int _itemCount;

            private static int ParentIndex(int i) => (i - 1) / 2;

            private static int RightChildIndex(int i) => 2 * i + 2;

            private static int LeftChildIndex(int i) => 2 * i + 1;

            private void BubbleUp(int i, ref NativeArray<PathNodeCalculation> nodes, ref NativeArray<int> indexes)
            {
                var parentIndex = ParentIndex(i);
                while (i > 0 && nodes[indexes[i]].F < nodes[indexes[parentIndex]].F)
                {
                    (indexes[i], indexes[parentIndex]) = (indexes[parentIndex], indexes[i]);
                    i = parentIndex;
                    parentIndex = ParentIndex(i);
                }
            }

            private void TrickleDown(int i, ref NativeArray<PathNodeCalculation> nodes, ref NativeArray<int> indexes)
            {
                do
                {
                    var j = -1;
                    var rightChildIndex = RightChildIndex(i);
                    var leftChildIndex = LeftChildIndex(i);

                    if (rightChildIndex < _itemCount && nodes[indexes[rightChildIndex]].F < nodes[indexes[i]].F)
                    {
                        j = nodes[indexes[leftChildIndex]].F < nodes[indexes[rightChildIndex]].F
                            ? leftChildIndex
                            : rightChildIndex;
                    }
                    else if (leftChildIndex < _itemCount && nodes[indexes[leftChildIndex]].F < nodes[indexes[i]].F)
                    {
                        j = leftChildIndex;
                    }

                    if (j >= 0)
                    {
                        (indexes[i], indexes[j]) = (indexes[j], indexes[i]);
                    }

                    i = j;
                } while (i >= 0);
            }
        }
    }
}