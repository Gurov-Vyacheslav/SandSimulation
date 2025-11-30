using SandSimulation.HalpStruct;
using System.Collections;
using System.Collections.Generic;
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

        private int[,,] _world;

        private Matrix4x4[] matrices;
        private List<Matrix4x4> visibleMatrices;

        private ChunkSimulation _chunkSimulation;

        public float VoxelScale { get; private set; }
        private Vector3 offset;


        private void Awake()
        {
            var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            voxelMesh = temp.GetComponent<MeshFilter>().sharedMesh;
            Destroy(temp);

            _world = new int[Size, Size, Size];

            matrices = new Matrix4x4[Size * Size * Size];
            visibleMatrices = new List<Matrix4x4>(Size * Size * Size);

            VoxelScale = 64f / Size;
            offset = new Vector3(-Size / 2f, 0, -Size / 2f) * VoxelScale;

            _chunkSimulation = new ChunkSimulation(_world, Size);
            PrecomputeMatrices();
            TestStartSand();

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
            for (int y = 0; y < Size; y++)
                _world[Size / 2, y, Size / 2] = 1;
        }

        private IEnumerator SimLoop()
        {
            while (true)
            {
                _chunkSimulation.Slip();
                Pipe.SimulatePour(_world);

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
                        if (_world[x, y, z] == 1)
                            visibleMatrices.Add(matrices[index]);
                        index++;
                    }
        }
    }
}
