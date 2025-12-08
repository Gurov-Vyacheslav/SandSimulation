using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace SandSimulation.HalpStruct
{
    [BurstCompile]
    internal struct SandSimulationJob : IJobParallelFor
    {
        [ReadOnly] public int Size;

        [NativeDisableParallelForRestriction]
        public NativeArray<int> World;

        public uint seed;

        // Настройка поведения
        const int PressureThreshold = 4;
        const float ProbabilitySlip = 0.2f;

        public void Execute(int index)
        {
            Translator.ToXYZ(index, Size, out int x, out int y, out int z);

            if (World[index] == 0 || y == 0)
                return;

            uint randomSeed = seed + (uint)index + 1u;
            if (randomSeed == 0) randomSeed = 1;
            Random random = new Random(randomSeed);

            if (TryMove(x, y, z, 0, -1, 0, index))
                return;

            int pressureHeight = CountSandAbove(x, y, z);

            if (random.NextInt(0, PressureThreshold + 2) > pressureHeight)
                return;

            int start = random.NextInt(0, 4);

            if (TryMoveDiagonally(start, x, y, z, index)) return;

            if (random.NextFloat() < ProbabilitySlip)
            {
                TrySlip(start, x, y, z, index);
            }
        }
        private bool TryMoveDiagonally(int start, int x, int y, int z, int index)
        {
            for (int k = 0; k < 4; k++)
            {
                int dir = (start + k) & 3;
                int dx = 0, dz = 0;

                switch (dir)
                {
                    case 0: dx = -1; break;
                    case 1: dx = 1; break;
                    case 2: dz = -1; break;
                    case 3: dz = 1; break;
                }

                if (TryMove(x, y, z, dx, -1, dz, index))
                    return true;
            }
            return false;
        }

        private void TrySlip(int start, int x, int y, int z, int index)
        {
            for (int k = 0; k < 4; k++)
            {
                int dir = (start + k) & 3;
                int sx = 0, sz = 0;

                switch (dir)
                {
                    case 0: sx = -1; break;
                    case 1: sx = 1; break;
                    case 2: sz = -1; break;
                    case 3: sz = 1; break;
                }

                if (TryMove(x, y, z, sx, 0, sz, index))
                    return;
            }
        }


        private bool TryMove(int x, int y, int z, int dx, int dy, int dz, int index)
        {
            int nx = x + dx;
            int ny = y + dy;
            int nz = z + dz;

            if (nx < 0 || ny < 0 || nz < 0 || nx >= Size || ny >= Size || nz >= Size)
                return false;

            int newIndex = Translator.ToIndex(nx, ny, nz, Size);

            if (World[newIndex] != 0)
                return false;

            World[index] = 0;
            World[newIndex] = 1;
            return true;
        }

        private int CountSandAbove(int x, int y, int z)
        {
            int count = 0;
            for (int yy = y + 1; yy < y + 1 + PressureThreshold && yy < Size; yy++)
            {
                int idx = Translator.ToIndex(x, yy, z, Size);
                if (World[idx] == 1)
                    count++;
                else break;
            }
            return count;
        }
    }

    internal class ChunkSimulationWrapper
    {
        private int _size;
        private NativeArray<int> _world;

        public ChunkSimulationWrapper(NativeArray<int> world, int size)
        {
            _size = size;
            _world = world;
        }

        public void Slip()
        {
            var job = new SandSimulationJob
            {
                Size = _size,
                World = _world,
                seed = (uint)UnityEngine.Random.Range(1, 1000000)
            };

            int totalVoxels = _size * _size * _size;
            JobHandle handle = job.Schedule(totalVoxels, totalVoxels/8);

            handle.Complete();
        }
  
    }
}