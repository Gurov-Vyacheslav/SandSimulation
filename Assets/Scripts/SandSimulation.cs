using SandSimulation.HalpStruct;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace SandSimulation
{
    [DefaultExecutionOrder(-100)]
    public class SandSimulation : MonoBehaviour
    {
        [field: SerializeField]
        public PipeController Pipe { get; private set; }
        [field: SerializeField]
        public int Size { get; private set; } = 64;
        [field: SerializeField]
        public float StepInterval { get; private set; } = 0.05f;
        [field: SerializeField]
        public Material VoxelMaterial { get; private set; }



        private Mesh voxelMesh;

        private NativeArray<int> _worldData;

        private Matrix4x4[] matrices;
        private List<Matrix4x4> visibleMatrices;

        private ChunkSimulationWrapper _chunkSimulation;

        public float VoxelScale { get; private set; }
        private Vector3 offset;

        private void Awake()
        {
            var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            voxelMesh = temp.GetComponent<MeshFilter>().sharedMesh;
            Destroy(temp);


            int totalSize = Size * Size * Size;
            _worldData = new NativeArray<int>(totalSize, Allocator.Persistent);
            for (int i = 0; i < totalSize; i++)
            {
                _worldData[i] = 0; 
            }


            matrices = new Matrix4x4[Size * Size * Size];
            visibleMatrices = new List<Matrix4x4>(Size * Size * Size);

            VoxelScale = 64f / Size;
            offset = new Vector3(-Size / 2f, 0, -Size / 2f) * VoxelScale;


            PrecomputeMatrices();
            TestStartSand();
            _chunkSimulation = new ChunkSimulationWrapper(_worldData, Size);

            StartCoroutine(SimLoop());
        }

        private void PrecomputeMatrices()
        {
            int index = 0;
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                    for (int z = 0; z < Size; z++)
                    {
                        Vector3 pos = new Vector3(x + 0.5f, y + 0.5f, z + 0.5f) * VoxelScale + offset;
                        matrices[index] = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one * VoxelScale);
                        index++;
                    }
        }

        private void TestStartSand()
        {
            int centerX = Size / 2;
            int centerZ = Size / 2;
            for (int y = 0; y < Size; y++)
            {
                _worldData[Translator.ToIndex(centerX, y, centerZ, Size)] = 1;
            }
        }

        private IEnumerator SimLoop()
        {
            while (true)
            {
                if (Input.GetKey(KeyCode.Q))
                {
                    for (int i = 0; i < Size*Size*Size; i++)
                    {
                        _worldData[i] = 0;
                    }
                }

                _chunkSimulation.Slip();

                Pipe.SimulatePour(_worldData);

                RefreshVisibleVoxels();

                yield return new WaitForSeconds(StepInterval);
            }
        }

        void Update()
        {
            int batchSize = 1023;
            for (int i = 0; i < visibleMatrices.Count; i += batchSize)
            {
                int count = Mathf.Min(batchSize, visibleMatrices.Count - i);
                Graphics.DrawMeshInstanced(
                    voxelMesh,
                    0,
                    VoxelMaterial,
                    visibleMatrices.GetRange(i, count).ToArray());
            }
        }

        private void RefreshVisibleVoxels()
        {
            int index = 0;
            visibleMatrices.Clear();
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                    for (int z = 0; z < Size; z++)
                    {
                        if (_worldData[Translator.ToIndex(x,y,z,Size)] == 1)
                            visibleMatrices.Add(matrices[index]);
                        index++;
                    }
        }
        private void OnDestroy()
        {
            if (_worldData.IsCreated) _worldData.Dispose();
        }
    }
}
