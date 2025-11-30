using System.Drawing;
using UnityEngine;


namespace SandSimulation
{
    public class PipeController : MonoBehaviour
    {
        [field: SerializeField]
        public SandSimulation SandSimulation { get; private set; }

        [field: SerializeField]
        public float MoveSpeed { get; private set; } = 5f;

        private float _sizeSpace;
        private void Start()
        {
            //_sizeSpace = SandSimulation.VoxelPrefab.transform.localScale.y * SandSimulation.Size;
            _sizeSpace = 10f;
            transform.position = new Vector3(0, _sizeSpace + 0.5f, 0);
        }


        private void Update()
        {
            float horizontalInput = Input.GetAxis("Horizontal");
            float verticalInput = Input.GetAxis("Vertical");

            Vector3 movement = new Vector3(horizontalInput, 0f, verticalInput) * MoveSpeed * Time.deltaTime;

            if (IsPositionValid(transform.position + movement))
            {
                transform.Translate(movement);
            }
        }

        private bool IsPositionValid(Vector3 position)
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

        public void SimulatePour(int[,,] mass)
        {
            if (!IsPours) return;

            int px = Mathf.FloorToInt(transform.position.x / SandSimulation.VoxelScale + SandSimulation.Size / 2f);
            int py = Mathf.FloorToInt(transform.position.y / SandSimulation.VoxelScale - 1f);
            int pz = Mathf.FloorToInt(transform.position.z / SandSimulation.VoxelScale + SandSimulation.Size / 2f);


            int pourRadius = Mathf.Max(Mathf.RoundToInt(1 / SandSimulation.VoxelScale) - 1, 0);
            int radiusSqr = pourRadius * pourRadius;

            int particlesPerFrame = 2 * pourRadius * (1 + pourRadius) + 1;

            for (int i = 0; i < particlesPerFrame; i++)
            {
                int offsetX = Random.Range(-pourRadius, pourRadius + 1);
                int offsetZ = Random.Range(-pourRadius, pourRadius + 1);

                int x = px + offsetX;
                int z = pz + offsetZ;

                int dx = x - px;
                int dz = z - pz;
                if (dx * dx + dz * dz <= radiusSqr)
                {
                    if (x >= 0 && x < SandSimulation.Size && py >= 0 && py < SandSimulation.Size && z >= 0 && z < SandSimulation.Size)
                    {
                        mass[x, py, z] = 1;
                    }

                }
            }
        }
    }
}
