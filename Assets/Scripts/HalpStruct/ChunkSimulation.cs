using System;
using System.Collections.Concurrent;
using System.Threading;
using Unity.Burst;
using UnityEditor;
using UnityEngine;


namespace SandSimulation.HalpStruct
{
    //[BurstCompile]
    internal class ChunkSimulation
    {
        private int[,,] _world;

        private readonly Int3[] _baseDirections = new Int3[]
        {
            new (0, -1, 0),    // вниз
            new (-1, -1, 0),   // влево вниз
            new (1, -1, 0),    // вправо вниз
            new (0, -1, -1),   // назад вниз
            new (0, -1, 1)     // вперед вниз
        };

        // Массивы направлений для каждого потока
        private Int3[][] _threadDirections;

        private readonly int _size;
        private readonly int _chunkLength;
        private readonly int _chunkCount = 8;

        private ConcurrentQueue<MoveCommand> _moveQueue;

        private System.Random _random =  new System.Random();

        
        public ChunkSimulation(int[,,] world, int size)
        {
            _world = world;
            _size = size;
            _chunkLength = size * size * size / _chunkCount;
            _moveQueue = new ConcurrentQueue<MoveCommand>();

            InitializeThreadDirections();
        }

        private void InitializeThreadDirections()
        {
            _threadDirections = new Int3[_chunkCount][];

            for (int i = 0; i < _chunkCount; i++)
            {
                // Создаем копию базовых направлений для каждого потока
                _threadDirections[i] = new Int3[_baseDirections.Length];
                Array.Copy(_baseDirections, _threadDirections[i], _baseDirections.Length);
            }
        }

        public void Slip()
        {
            var threads = new Thread[_chunkCount];
            for (int i = 0; i < _chunkCount; i++)
            {
                int chunkIndex = i;
                threads[chunkIndex] = new Thread(() =>
                {
                    SlipChunk(chunkIndex);
                });

                threads[i].Start();
            }
            foreach (var thread in threads)
            {
                thread.Join();
            }
            //ApplyMoveQueueToWorld();

        }

        private void SlipChunk(int chunkIndex)
        {
            int startIndex = chunkIndex * _chunkLength;
            int endIndex = Math.Min(startIndex + _chunkLength, _size * _size * _size);
            int x = 0, y = 0, z = 0;

            Int3[] directions = _threadDirections[chunkIndex];

            for (int i = startIndex; i < endIndex; i++)
            {
                ToXYZ(i, ref x, ref y, ref z);
                if (_world[x,y,z] == 0) continue;

                Shuffle(directions);

                for (int di = 0; di < directions.Length; di++)
                {
                    Int3 d = directions[di];
                    int nx = x + d.x;
                    int ny = y + d.y;
                    int nz = z + d.z;

                    if (!IsInBounds(nx, ny, nz)) continue;

                    if (_world[nx, ny, nz] == 0)
                    {
                        _world[nx, ny, nz] = 1;
                        _world[x, y, z] = 0;
                        break;
                    }
                }
                    
            }
        }

        private void ToXYZ(int index, ref int x, ref int y, ref int z)
        {
            x = index % _size;
            z = (index / _size) % _size;
            y = index / (_size * _size);
        }

        private int ToIndex(int x, int y, int z)
        {
            return x + z * _size + y * _size * _size;
        }

        private bool IsInBounds(int x, int y, int z)
        {
            return x >= 0 && x < _size && y >= 0 && y < _size && z >= 0 && z < _size;
        }
        static private bool BelongsToChunk(int idx, int startIndex, int endIndex)
        {
            return idx >= startIndex && idx < endIndex;
        }
        private void Shuffle(Int3[] arr)
        {
            for (int i = arr.Length - 1; i > 1; i--)
            {
                int j = _random.Next(1, i + 1);
                (arr[i], arr[j]) = (arr[j], arr[i]);
            }
        }

        private void ApplyMoveQueueToWorld()
        {

            while (_moveQueue.TryDequeue(out MoveCommand cmd))
            {
                if (_world[cmd.to.x, cmd.to.y, cmd.to.z] == 0)
                {
                    _world[cmd.to.x, cmd.to.y, cmd.to.z] = 1;
                    _world[cmd.from.x, cmd.from.y, cmd.from.z] = 0;
                }
            }
        }

    }
}
