using Unity.Collections;
using UnityEngine;
using SandSimulation.HalpStruct;

namespace SandSimulation
{
    public class PipeController : MonoBehaviour
    {
        [field: SerializeField]
        public SandSimulation SandSimulation { get; private set; }

        [field: SerializeField]
        public float MoveSpeed { get; private set; } = 5f;

        [field: SerializeField]
        public int SizePourRadius { get; private set; } = 4;
        [field: SerializeField]
        public float SizeSpaceMovement { get; private set; } = 60f;

        private int _maxPourRadius;
        private int _lastPourRadius = 2;

        private void Start()
        {
            transform.position = new Vector3(0, SizeSpaceMovement/2, 0);

            _maxPourRadius = Mathf.Max(Mathf.RoundToInt(1 / SandSimulation.VoxelScale) - 1, 0) * SizePourRadius;
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
            return position.x >= -SizeSpaceMovement / 2 && position.x <= SizeSpaceMovement / 2 &&
                   position.z >= -SizeSpaceMovement / 2 && position.z <= SizeSpaceMovement / 2;
        }

        public bool IsPours
        {
            get
            {
                return Input.GetKey(KeyCode.Space);
            }
        }

        public void SimulatePour(NativeArray<int> mass)
        {
            int pourRadius;
       
            if (IsPours)
            {
                pourRadius = Mathf.Min(_maxPourRadius, _lastPourRadius + 1);
            } else
            {
                if (_lastPourRadius <= 2) return;
                pourRadius = _lastPourRadius - 1;
            }
            _lastPourRadius = pourRadius;

            int px = Mathf.FloorToInt(transform.position.x / 
                SandSimulation.VoxelScale + SandSimulation.Size / 2f);
            int py = Mathf.FloorToInt(transform.position.y / 
                SandSimulation.VoxelScale - 1f);
            int pz = Mathf.FloorToInt(transform.position.z / 
                SandSimulation.VoxelScale + SandSimulation.Size / 2f);

            float radiusSqr = pourRadius * pourRadius;

            int particlesPerFrame = 2 * pourRadius * (1 + pourRadius) + 1;
            particlesPerFrame *= 2;

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
                    if (x >= 0 && x < SandSimulation.Size && 
                        py >= 0 && py < SandSimulation.Size && 
                        z >= 0 && z < SandSimulation.Size)
                    {
                        mass[Translator.ToIndex(x, py, z, SandSimulation.Size)] = 1;
                    }

                }
            }
        }
    }
}
