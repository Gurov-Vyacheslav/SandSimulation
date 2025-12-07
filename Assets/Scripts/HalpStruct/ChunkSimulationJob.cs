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

        // Тонкая настройка поведения
        const int MaxPressureScanHeight = 8;     // насколько далеко вверх смотреть столб
        const int PressureThreshold = 5;     // сколько песка сверху нужно для "давления"
        const float PressureSideSlideChance = 0.4f;  // шанс бокового сдвига под 

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

            int start = random.NextInt(0, 4);

            for (int k = 0; k < 4; k++)
            {
                int dir = (start + k) & 3; // dir = 0..3 по кругу
                int dx = 0, dz = 0;

                switch (dir)
                {
                    case 0: dx = -1; break;
                    case 1: dx = 1; break;
                    case 2: dz = -1; break;
                    case 3: dz = 1; break;
                }

                if (TryMove(x, y, z, dx, -1, dz, index))
                    return;
            }

            // 4. Давление сверху: считаем высоту столба песка над текущей клеткой
            if (random.NextFloat() > PressureSideSlideChance) return;
            
            int pressureHeight = 0;
            int maxYScan = math.min(Size, y + 1 + MaxPressureScanHeight);

            for (int yy = y + 1; yy < maxYScan; yy++)
            {
                int aboveIdx = Translator.ToIndex(x, yy, z, Size);
                if (World[aboveIdx] != 0)
                    pressureHeight++;
                else
                    break;
            }

            if (pressureHeight >= PressureThreshold)
            {
                // 4 возможных направления по горизонтали
                int dirH = random.NextInt(0, 4);
                int hdx = 0, hdz = 0;

                switch (dirH)
                {
                    case 0: hdx = -1; break;
                    case 1: hdx = 1; break;
                    case 2: hdz = -1; break;
                    case 3: hdz = 1; break;
                }

                // Горизонтальный шаг, чтобы песок мог "ползти" с перегруженного места
                if (TryMove(x, y, z, hdx, 0, hdz, index))
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