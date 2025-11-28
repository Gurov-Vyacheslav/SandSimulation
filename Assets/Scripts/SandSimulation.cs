using System.Collections;
using UnityEngine;

namespace SandSimulation
{
    [DefaultExecutionOrder(-100)]
    public class SandSimulation : MonoBehaviour
    {
        [field: SerializeField] public int Size { get; private set; } = 64;
        [field: SerializeField] public GameObject VoxelPrefab { get; private set; }
        [SerializeField] private float stepInterval = 0.1f;
        [SerializeField] private PipeController _pipeController;

        private int[,,] _mass;
        private int[,,] _oldMass;
        private GameObject[,,] _voxels;

        private float _voxelScale;


        private void Awake()
        {
            _mass = new int[Size, Size, Size];
            _voxels = new GameObject[Size, Size, Size];
            _oldMass = new int[Size, Size, Size];

            _voxelScale = 64f / Size;
            VoxelPrefab.transform.localScale = new Vector3(_voxelScale, _voxelScale, _voxelScale);

            SpawnVoxels();
            TestStartSend();
            RefreshVoxels();

            StartCoroutine(SimLoop());
        }
        IEnumerator SimLoop()
        {
            while (true)
            {
                System.Buffer.BlockCopy(_mass, 0, _oldMass, 0, _mass.Length * sizeof(int));

                Simulate();
                RefreshVoxels();
                yield return new WaitForSeconds(stepInterval);
            }
        }
        private void TestStartSend()
        {
            for (int y = 0; y < Size; y++)
                _mass[Size / 2, y, Size / 2] = 1;
        }

        private void SpawnVoxels()
        {
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                    for (int z = 0; z < Size; z++)
                    {
                        GameObject voxel = Instantiate(VoxelPrefab, new Vector3(-Size / 2 + x + 0.5f, y + 0.5f, -Size / 2 + z + 0.5f) * _voxelScale, Quaternion.identity, transform);
                        voxel.SetActive(false);
                        _voxels[x, y, z] = voxel;
                    }
        }

        private Vector3Int[] directions = new Vector3Int[]
                       {
                        new Vector3Int(-1, -1, 0),
                        new Vector3Int(1, -1, 0),
                        new Vector3Int(0, -1, -1),
                        new Vector3Int(0, -1, 1),
                       };

        private System.Random rng = new System.Random();
        private void Simulate()
        {

            for (int y = 1; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    for (int z = 0; z < Size; z++)
                    {
                        if (_mass[x, y, z] == 0) continue;

                        if (IsEmpty(x, y - 1, z))
                        {
                            _mass[x, y, z] = 0;
                            _mass[x, y - 1, z] = 1;
                            continue;
                        }

                        for (int i = directions.Length - 1; i > 0; i--)
                        {
                            int j = rng.Next(i + 1);
                            var temp = directions[i];
                            directions[i] = directions[j];
                            directions[j] = temp;
                        }

                        foreach (var dir in directions)
                        {
                            int nx = x + dir.x;
                            int ny = y + dir.y;
                            int nz = z + dir.z;

                            if (IsEmpty(nx, ny, nz))
                            {
                                _mass[x, y, z] = 0;
                                _mass[nx, ny, nz] = 1;
                                break;
                            }
                        }
                    }
                }
            }
            SimulatePour();
        }

        private bool IsEmpty(int x, int y, int z)
        {
            return x >= 0 && x < Size && y >= 0 && y < Size && z >= 0 && z < Size && _mass[x, y, z] == 0;
        }

        private void RefreshVoxels()
        {
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                    for (int z = 0; z < Size; z++)
                        if (_oldMass[x, y, z] != _mass[x, y, z])
                            _voxels[x, y, z].SetActive(_mass[x, y, z] == 1);
        }
        private void SimulatePour()
        {
            int px = (int)(_pipeController.transform.position.x / _voxelScale + Size / 2);
            int py = (int)(_pipeController.transform.position.y / _voxelScale - 1f);
            int pz = (int)(_pipeController.transform.position.z / _voxelScale + Size / 2);

            if (_pipeController.IsPours) _mass[px, py, pz] = 1;
        }


    }
}
