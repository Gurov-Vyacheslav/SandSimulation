using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace SandSimulation.HalpStruct
{
    //[BurstCompile]
    internal struct ChunkSimJob: IJobParallelFor
    {
        [ReadOnly] public NativeArray<int> worldOld;

        [NativeDisableParallelForRestriction]
        [WriteOnly] public NativeArray<int> worldNew;

        [ReadOnly] public NativeArray<Int3> directionsArr;

        public int size;
        public int chunkSize;
        public int chunksPerAxis;
        public int chunkCount;

        public NativeQueue<MoveCommand>.ParallelWriter moveQueue;


        public void Execute(int chunkIndex)
        {
            int cx = chunkIndex % chunksPerAxis;
            int cy = (chunkIndex / chunksPerAxis) % chunksPerAxis;
            int cz = chunkIndex / (chunksPerAxis * chunksPerAxis);

            int startX = cx * chunkSize;
            int startY = cy * chunkSize;
            int startZ = cz * chunkSize;

            int endX = Math.Min(startX + chunkSize, size);
            int endY = Math.Min(startY + chunkSize, size);
            int endZ = Math.Min(startZ + chunkSize, size);

            for (int y = startY + 1; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    for (int z = startZ; z < endZ; z++)
                    {
                        int idx = ToIndex(x, y, z, size);
                        if (worldOld[idx] == 0) continue;

                        bool moved = false;
                        for (int di = 0; di < directionsArr.Length; di++)
                        {
                            Int3 d = directionsArr[di];
                            int nx = x + d.x;
                            int ny = y + d.y;
                            int nz = z + d.z;
                            if (!IsInBounds(nx, ny, nz, size)) continue;
                            if (!IsInBounds(nx, ny, nz, size)) continue;

                            int destIdx = ToIndex(nx, ny, nz, size);
                            if (BelongsToChunk(destIdx, chunkSize, size, startX, startY, startZ))
                            {
                                if (worldOld[destIdx] == 0)
                                {
                                    Debug.Log($"destIdx: {destIdx}, worldNew.Length: {worldNew.Length}");

                                    worldNew[destIdx] = 1;
                                    moved = true;
                                    break;
                                }
                            }
                            else
                            {
                                if (worldOld[destIdx] == 0)
                                {
                                    moveQueue.Enqueue(new MoveCommand { from = idx, to = destIdx });
                                    moved = true;
                                    break;
                                }
                            }
                        }

                        if (!moved) worldNew[idx] = 1;
                    }
                }
            }
        }

        static int ToIndex(int x, int y, int z, int size)
        {
            return x + y * size + z * size * size;
        }

        static bool IsInBounds(int x, int y, int z, int size)
        {
            return x >= 0 && x < size && y >= 0 && y < size && z >= 0 && z < size;
        }
        static bool BelongsToChunk(int idx, int chunkSize, int size, int startX, int startY, int startZ)
        {
            int x = idx % size;
            int y = (idx / size) % size;
            int z = idx / (size * size);
            return x >= startX && x < startX + chunkSize
                && y >= startY && y < startY + chunkSize
                && z >= startZ && z < startZ + chunkSize;
        }
    }
}
