using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace CrystalOutpost.Sim
{
    public struct NavGrid : IDisposable
    {
        public const byte Blocked = 0;
        public const byte PathCost = 1;
        public const byte GroundCost = 3;

        public int Width;
        public int Height;
        public float CellSize;
        public float2 Origin;

        public NativeArray<byte> Cost;
        public NativeArray<float2> Flow;
        public NativeArray<float> Distance;

        public int CellCount => Width * Height;

        public NavGrid(int width, int height, float cellSize, float2 origin, Allocator allocator)
        {
            Width = width;
            Height = height;
            CellSize = cellSize;
            Origin = origin;
            Cost = new NativeArray<byte>(width * height, allocator);
            Flow = new NativeArray<float2>(width * height, allocator);
            Distance = new NativeArray<float>(width * height, allocator);

            for (int i = 0; i < Cost.Length; i++)
                Cost[i] = GroundCost;
        }

        public float2 Min => Origin;
        public float2 Max => Origin + new float2(Width, Height) * CellSize;

        public int2 CellOf(float2 position)
        {
            float2 local = (position - Origin) / CellSize;
            return new int2(
                math.clamp((int)math.floor(local.x), 0, Width - 1),
                math.clamp((int)math.floor(local.y), 0, Height - 1));
        }

        public int IndexOf(float2 position)
        {
            int2 cell = CellOf(position);
            return cell.y * Width + cell.x;
        }

        public float2 CellCenter(int x, int y) => Origin + (new float2(x, y) + 0.5f) * CellSize;

        public bool IsBlocked(float2 position) => Cost[IndexOf(position)] == Blocked;

        public void Paint(float2 center, float radius, byte cost)
        {
            int2 min = CellOf(center - radius);
            int2 max = CellOf(center + radius);
            for (int y = min.y; y <= max.y; y++)
            {
                for (int x = min.x; x <= max.x; x++)
                {
                    if (math.distancesq(CellCenter(x, y), center) <= radius * radius)
                        Cost[y * Width + x] = cost;
                }
            }
        }

        public void Dispose()
        {
            if (Cost.IsCreated) Cost.Dispose();
            if (Flow.IsCreated) Flow.Dispose();
            if (Distance.IsCreated) Distance.Dispose();
        }
    }

    [BurstCompile]
    public struct FlowFieldJob : IJob
    {
        public int Width;
        public int Height;
        public float CellSize;
        public float2 Origin;
        public float2 Goal;
        public float GoalRadius;

        [ReadOnly] public NativeArray<byte> Cost;
        public NativeArray<float> Distance;
        public NativeArray<float2> Flow;

        private struct Node
        {
            public float Distance;
            public int Cell;
        }

        public void Execute()
        {
            int count = Width * Height;
            var heap = new NativeList<Node>(count, Allocator.Temp);

            for (int i = 0; i < count; i++)
            {
                Distance[i] = float.MaxValue;
                Flow[i] = float2.zero;
            }

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    float2 center = Origin + (new float2(x, y) + 0.5f) * CellSize;
                    if (math.distance(center, Goal) > GoalRadius) continue;

                    int cell = y * Width + x;
                    Distance[cell] = 0f;
                    Push(ref heap, new Node { Distance = 0f, Cell = cell });
                }
            }

            while (heap.Length > 0)
            {
                Node node = Pop(ref heap);
                if (node.Distance > Distance[node.Cell]) continue;

                int cx = node.Cell % Width;
                int cy = node.Cell / Width;

                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = cx + dx;
                        int ny = cy + dy;
                        if (nx < 0 || ny < 0 || nx >= Width || ny >= Height) continue;

                        int neighbour = ny * Width + nx;
                        byte cost = Cost[neighbour];
                        if (cost == NavGrid.Blocked) continue;

                        if (dx != 0 && dy != 0 && !DiagonalOpen(cx, cy, dx, dy)) continue;

                        float step = (dx != 0 && dy != 0 ? 1.41421356f : 1f) * cost;
                        float candidate = node.Distance + step;
                        if (candidate >= Distance[neighbour]) continue;

                        Distance[neighbour] = candidate;
                        Push(ref heap, new Node { Distance = candidate, Cell = neighbour });
                    }
                }
            }

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    int cell = y * Width + x;
                    if (Cost[cell] == NavGrid.Blocked || Distance[cell] == 0f || Distance[cell] == float.MaxValue)
                        continue;

                    float best = Distance[cell];
                    int2 bestDir = int2.zero;

                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            int nx = x + dx;
                            int ny = y + dy;
                            if (nx < 0 || ny < 0 || nx >= Width || ny >= Height) continue;
                            if (dx != 0 && dy != 0 && !DiagonalOpen(x, y, dx, dy)) continue;

                            float d = Distance[ny * Width + nx];
                            if (d < best)
                            {
                                best = d;
                                bestDir = new int2(dx, dy);
                            }
                        }
                    }

                    if (bestDir.x != 0 || bestDir.y != 0)
                        Flow[cell] = math.normalize(new float2(bestDir));
                }
            }

            heap.Dispose();
        }

        private bool DiagonalOpen(int x, int y, int dx, int dy)
        {
            return IsWalkableOrGoal(x + dx, y) && IsWalkableOrGoal(x, y + dy);
        }

        private bool IsWalkableOrGoal(int x, int y)
        {
            int cell = y * Width + x;
            return Cost[cell] != NavGrid.Blocked || Distance[cell] == 0f;
        }

        private static void Push(ref NativeList<Node> heap, Node node)
        {
            heap.Add(node);
            int i = heap.Length - 1;
            while (i > 0)
            {
                int parent = (i - 1) >> 1;
                if (heap[parent].Distance <= heap[i].Distance) break;
                Node tmp = heap[parent];
                heap[parent] = heap[i];
                heap[i] = tmp;
                i = parent;
            }
        }

        private static Node Pop(ref NativeList<Node> heap)
        {
            Node top = heap[0];
            int last = heap.Length - 1;
            heap[0] = heap[last];
            heap.RemoveAt(last);

            int i = 0;
            while (true)
            {
                int left = i * 2 + 1;
                int right = left + 1;
                int smallest = i;
                if (left < heap.Length && heap[left].Distance < heap[smallest].Distance) smallest = left;
                if (right < heap.Length && heap[right].Distance < heap[smallest].Distance) smallest = right;
                if (smallest == i) break;

                Node tmp = heap[smallest];
                heap[smallest] = heap[i];
                heap[i] = tmp;
                i = smallest;
            }

            return top;
        }
    }
}
