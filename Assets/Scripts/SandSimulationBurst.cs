using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using SandSimulation.HalpStruct;

namespace SandSimulation
{
    [DefaultExecutionOrder(-100)]
    public class SandSimulationBurst : MonoBehaviour
    {
        [Header("Grid settings")]
        [SerializeField] public int Size = 64;             // должно быть кратно ChunkSize
        [SerializeField] public int ChunkSize = 16;        // размер чанка (16 рекомендовано)
        [SerializeField] public GameObject VoxelPrefab;
        [SerializeField] public float StepInterval = 0.1f; // сек/шаг

        [Header("External")]
        [SerializeField] private PipeController PipeController;

        // Flattened world arrays (NativeArrays)
        private NativeArray<int> worldOld; // читается в job'ах
        private NativeArray<int> worldNew; // пишется в job'ах
        private int voxelsTotal;

        // GameObject-рендер (по-прежнему используем воксели, но обновляем только изменившиеся)
        private GameObject[,,] voxelsGO;
        private float voxelScale;

        // Derived
        private int chunksPerAxis;
        private int totalChunks;

        // Job-specific
        private JobHandle simJobHandle;
        private NativeQueue<MoveCommand> moveQueue;

        private NativeArray<Int3> directionsNativePersistent;


        #region Unity lifecycle

        private void Awake()
        {
            if (Size % ChunkSize != 0)
            {
                Debug.LogWarning("Size should be divisible by ChunkSize. Adjusting Size to nearest multiple.");
                int cp = Mathf.Max(1, Size / ChunkSize);
                Size = cp * ChunkSize;
            }

            chunksPerAxis = Size / ChunkSize;
            totalChunks = chunksPerAxis * chunksPerAxis * chunksPerAxis;
            voxelsTotal = Size * Size * Size;

            directionsNativePersistent = new NativeArray<Int3>(Int3.Directions.Length, Allocator.Persistent);
            for (int i = 0; i < Int3.Directions.Length; i++)
                directionsNativePersistent[i] = Int3.Directions[i];

            worldOld = new NativeArray<int>(voxelsTotal, Allocator.Persistent);
            worldNew = new NativeArray<int>(voxelsTotal, Allocator.Persistent);

            for (int i = 0; i < voxelsTotal; i++) { worldOld[i] = 0; worldNew[i] = 0; }

            voxelScale = 64f / Size;
            if (VoxelPrefab == null) Debug.LogError("Assign VoxelPrefab in inspector.");

            InitializeVoxelGOs();

            int mid = Size / 2;
            for (int y = 0; y < Size; y++)
            {
                int idx = ToIndex(mid, y, mid);
                worldOld[idx] = 1;
            }

            RefreshAllVoxelsFromNative(worldOld);

            moveQueue = new NativeQueue<MoveCommand>(Allocator.Persistent);

            InvokeRepeating(nameof(StepSimulation), 0f, StepInterval);
        }

        private void OnDestroy()
        {
            CancelInvoke(nameof(StepSimulation));

            simJobHandle.Complete();

            if (worldOld.IsCreated) worldOld.Dispose();
            if (worldNew.IsCreated) worldNew.Dispose();
            if (moveQueue.IsCreated) moveQueue.Dispose();
            if (directionsNativePersistent.IsCreated) directionsNativePersistent.Dispose();
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(StepSimulation));

            if (simJobHandle.IsCompleted == false) simJobHandle.Complete();

            if (moveQueue.IsCreated) moveQueue.Dispose();
            if (worldOld.IsCreated) worldOld.Dispose();
            if (worldNew.IsCreated) worldNew.Dispose();
            if (directionsNativePersistent.IsCreated) directionsNativePersistent.Dispose();
        }


        #endregion

        #region Simulation loop

        private void StepSimulation()
        {
            for (int i = 0; i < voxelsTotal; i++) worldNew[i] = 0;

            while (moveQueue.TryDequeue(out _)) { }

            var job = new ChunkSimJob()
            {
                worldOld = worldOld,
                worldNew = worldNew,
                size = Size,
                chunkSize = ChunkSize,
                chunksPerAxis = chunksPerAxis,
                chunkCount = totalChunks,
                moveQueue = moveQueue.AsParallelWriter(),
                directionsArr = directionsNativePersistent
            };

            simJobHandle = job.Schedule(totalChunks, 1);
            simJobHandle.Complete();


            ApplyMoveQueueToWorldNew();

            SimulatePourToWorldNew();

            RefreshChangedVoxelsAndSwap();
        }

        #endregion

        #region Helper routines & conversions
        private static int ToIndex(int x, int y, int z)
        {
            return x + y * InstanceSize + z * InstanceSize * InstanceSize;
        }

        private static int InstanceSize;
        private void InitializeVoxelGOs()
        {
            InstanceSize = Size;

            voxelsGO = new GameObject[Size, Size, Size];
            Vector3 originOffset = new Vector3(-Size / 2f, 0f, -Size / 2f) * voxelScale;

            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                    for (int z = 0; z < Size; z++)
                    {
                        Vector3 pos = originOffset + new Vector3(x + 0.5f, y + 0.5f, z + 0.5f) * voxelScale;
                        GameObject go = Instantiate(VoxelPrefab, pos, Quaternion.identity, transform);
                        go.transform.localScale = Vector3.one * voxelScale;
                        go.SetActive(false);
                        voxelsGO[x, y, z] = go;
                    }
        }

        private void RefreshAllVoxelsFromNative(NativeArray<int> arr)
        {
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                    for (int z = 0; z < Size; z++)
                    {
                        int idx = x + y * Size + z * Size * Size;
                        voxelsGO[x, y, z].SetActive(arr[idx] != 0);
                    }
        }

        private void ApplyMoveQueueToWorldNew()
        {
            while (moveQueue.TryDequeue(out MoveCommand cmd))
            {
                if (cmd.to >= 0 && cmd.to < worldNew.Length)
                {
                    if (worldNew[cmd.to] == 0)
                    {
                        worldNew[cmd.to] = 1;
                        if (cmd.from >= 0 && cmd.from < worldNew.Length)
                            worldNew[cmd.from] = 0;
                    }
                    else
                    {
                        if (cmd.from >= 0 && cmd.from < worldNew.Length)
                            worldNew[cmd.from] = 1;
                    }
                }
            }
        }

        private void SimulatePourToWorldNew()
        {
            if (PipeController == null) return;

            int px = Mathf.FloorToInt(PipeController.transform.position.x / voxelScale + Size / 2f);
            int py = Mathf.FloorToInt(PipeController.transform.position.y / voxelScale - 1f);
            int pz = Mathf.FloorToInt(PipeController.transform.position.z / voxelScale + Size / 2f);

            if (px >= 0 && px < Size && py >= 0 && py < Size && pz >= 0 && pz < Size)
            {
                if (PipeController.IsPours)
                {
                    int idx = px + py * Size + pz * Size * Size;
                    worldNew[idx] = 1;
                }
            }
        }

        private void RefreshChangedVoxelsAndSwap()
        {
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                    for (int z = 0; z < Size; z++)
                    {
                        int idx = x + y * Size + z * Size * Size;
                        int oldVal = worldOld[idx];
                        int newVal = worldNew[idx];
                        if (oldVal != newVal)
                        {
                            voxelsGO[x, y, z].SetActive(newVal != 0);
                        }
                    }
            NativeArray<int>.Copy(worldNew, worldOld);
        }

        #endregion
    }
}
