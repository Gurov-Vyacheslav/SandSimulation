using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;


public class SandSimulation : MonoBehaviour
{
    [field: SerializeField]
    public int Size { get; private set; } = 64;
    [field: SerializeField]
    public GameObject VoxelPrefab { get; private set; }

    [SerializeField] 
    private float stepInterval = 0.1f;
    private float stepTimer = 0f;

    private int[,,] _mass;
    private GameObject[,,] _voxels;

    private float _voxelScale;

    [SerializeField]
    private PipeController _pipeController;


    void Awake()
    {
        _mass = new int[Size, Size, Size];
        _voxels = new GameObject[Size, Size, Size];
        _voxelScale = 64f/Size;
        VoxelPrefab.transform.localScale = new Vector3(_voxelScale, _voxelScale, _voxelScale);

        SpawnVoxels();

        for (int y = 0; y < Size; y++)
            _mass[Size / 2, y, Size / 2] = 1;

        RefreshVoxels();
    }

    void SpawnVoxels()
    {
        for (int x = 0; x < Size; x++)
            for (int y = 0; y < Size; y++)
                for (int z = 0; z < Size; z++)
                {
                    GameObject voxel = Instantiate(VoxelPrefab, new Vector3(-Size/2 + x +0.5f, y + 0.5f, -Size / 2 + z + 0.5f) * _voxelScale, Quaternion.identity, transform);
                    voxel.SetActive(false);
                    _voxels[x, y, z] = voxel;
                }
    }

    void Update()
    {
        stepTimer += Time.deltaTime;

        if (stepTimer >= stepInterval)
        {
            stepTimer = 0f;     
            Simulate();        
            RefreshVoxels();     
        }
    }

    Vector3Int[] directions = new Vector3Int[]
                   {
                        new Vector3Int(-1, -1, 0),   
                        new Vector3Int(1, -1, 0),    
                        new Vector3Int(0, -1, -1),  
                        new Vector3Int(0, -1, 1),   
                   };

    System.Random rng = new System.Random();
    void Simulate()
    {
        int[,,] newMass = new int[Size, Size, Size];

        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size; y++)
            {
                for (int z = 0; z < Size; z++)
                {
                    if (_mass[x, y, z] == 0) continue;
                    
                    if (IsEmpty(x, y - 1, z)) 
                    { 
                        newMass[x, y - 1, z] = 1; 
                        continue; 
                    }

                    for (int i = directions.Length - 1; i > 0; i--)
                    {
                        int j = rng.Next(i + 1);
                        var temp = directions[i];
                        directions[i] = directions[j];
                        directions[j] = temp;
                    }

                    bool moved = false;
                    foreach (var dir in directions)
                    {
                        int nx = x + dir.x;
                        int ny = y + dir.y;
                        int nz = z + dir.z;

                        if (IsEmpty(nx, ny, nz))
                        {
                            newMass[nx, ny, nz] = 1;
                            moved = true;
                            break;
                        }
                    }

                    if (!moved) newMass[x, y, z] = 1;
                }
            }
        }

        _mass = newMass;
        SimulatePour();
    }

    bool IsEmpty(int x, int y, int z)
    {
        return x >= 0 && x < Size && y >= 0 && y < Size && z >= 0 && z < Size && _mass[x, y, z] == 0;
    }

    void RefreshVoxels()
    {
        for (int x = 0; x < Size; x++)
            for (int y = 0; y < Size; y++)
                for (int z = 0; z < Size; z++)
                    _voxels[x, y, z].SetActive(_mass[x, y, z] > 0);
    }
    void SimulatePour()
    {
        _mass[(int)(_pipeController.transform.position.x / _voxelScale + Size / 2),
            (int)(_pipeController.transform.position.y / _voxelScale - 1f),
            (int)(_pipeController.transform.position.z / _voxelScale + Size / 2)] = _pipeController.IsPours ? 1 : 0;
    }


}
