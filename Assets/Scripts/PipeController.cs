using UnityEngine;

public class PipeController : MonoBehaviour
{
    [field:SerializeField]
    public SandSimulation SandSimulation { get; private set; }

    [field: SerializeField]
    public float MoveSpeed { get; private set; } = 5f;

    private float _sizeSpace;
    void Start()
    {
        //_sizeSpace = SandSimulation.VoxelPrefab.transform.localScale.y * SandSimulation.Size;
        _sizeSpace = 10f;
        transform.position = new Vector3(0, _sizeSpace+0.5f, 0);
    }

 
    void Update()
    {
        float horizontalInput = Input.GetAxis("Horizontal"); 
        float verticalInput = Input.GetAxis("Vertical"); 

        Vector3 movement = new Vector3(horizontalInput, 0f, verticalInput) * MoveSpeed * Time.deltaTime;

        if (IsPositionValid(transform.position + movement))
        {
            transform.Translate(movement);
        }
    }

    bool IsPositionValid(Vector3 position)
    {
        return position.x >= -_sizeSpace / 2 && position.x <= _sizeSpace / 2 &&
               position.z >= -_sizeSpace / 2 && position.z <= _sizeSpace / 2;
    }

    public bool IsPours
    {
        get
        {
            return Input.GetKey(KeyCode.Space);
        }
    }
}
