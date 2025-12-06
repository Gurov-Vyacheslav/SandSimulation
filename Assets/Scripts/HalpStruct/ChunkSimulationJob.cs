using System;
using System.Drawing;
using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace SandSimulation.HalpStruct
{
    //[BurstCompile]
    internal struct SandSimulationJob : IJobParallelFor
    {
        [ReadOnly] public int Size;

        [NativeDisableParallelForRestriction]
        public NativeArray<int> World;

        public uint seed;
        public NativeArray<int> seted;

        public void Execute(int index)
        {
            // Преобразуем линейный индекс в координаты
            int y = index / (Size * Size);
            int z = (index / Size) % Size;
            int x = index % Size;

            Unity.Mathematics.Random random = new(seed + (uint)index * 1000);

            seted[index] = 1;
            if (World[index] == 0 || y == 0) return;

            // Проверяем все возможные направления падения
            int3[] directions = new int3[]
            {
                new (0, -1, 0),    // вниз
                new (-1, -1, 0),   // вниз-влево
                new (1, -1, 0),    // вниз-вправо
                new (0, -1, -1),   // вниз-назад
                new (0, -1, 1)     // вниз-вперед
            };

            // Перемешиваем направления
            for (int i = directions.Length - 1; i > 1; i--)
            {
                int j = random.NextInt(1, i + 1);
                (directions[i], directions[j]) = (directions[j], directions[i]);
            }

            // Пытаемся найти свободное место для падения
            foreach (var dir in directions)
            {
                int nx = x + dir.x;
                int ny = y + dir.y;
                int nz = z + dir.z;

                if (nx < 0 || nx >= Size || ny < 0 || ny >= Size || nz < 0 || nz >= Size)
                    continue;

                int newIndex = nx + nz * Size + ny * Size * Size;

                if (World[newIndex] == 0)
                {
                    World[index] = 0;
                    World[newIndex] = 1;
                    return;
                }
            }
        }
    }

    internal class ChunkSimulationWrapper
    {
        private int _size;
        private int[,,] _world;
        private NativeArray<int> _buffer;
        private NativeArray<int> _seted;

        public ChunkSimulationWrapper(int[,,] world, int size)
        {
            _size = size;
            _world = world;

            int totalSize = size * size * size;
            _buffer = new NativeArray<int>(totalSize, Allocator.Persistent);
            _seted = new NativeArray<int>(totalSize, Allocator.Persistent);
            UpdateNativeArraysFromWorld();

            int count = 0;
            foreach (var item in _buffer)
            {
                count += item;
            }
            Debug.Log(count);
            for (int x = 0; x < _size; x++)
                for (int y = 0; y < _size; y++)
                    for (int z = 0; z < _size; z++)
                        count += world[x, y, z];
            Debug.Log(count);
        }

        public void Slip()
        {
            UpdateNativeArraysFromWorld();

            var job = new SandSimulationJob
            {
                seted = _seted,
                Size = _size,
                World = _buffer,
                seed = (uint)UnityEngine.Random.Range(1, 1000000)
            };

            int totalVoxels = _size * _size * _size;
            JobHandle handle = job.Schedule(totalVoxels, totalVoxels/8);

            handle.Complete();

            

            UpdateWorldFromNativeArray();
        }
        private void UpdateNativeArraysFromWorld()
        {
            for (int x = 0; x < _size; x++)
                for (int y = 0; y < _size; y++)
                    for (int z = 0; z < _size; z++)
                    {
                        int index = x + z * _size + y * _size * _size;
                        int value = _world[x, y, z];
                        _buffer[index] = value;;
                    }
        }

        private void UpdateWorldFromNativeArray()
        {
            for (int x = 0; x < _size; x++)
                for (int y = 0; y < _size; y++)
                    for (int z = 0; z < _size; z++)
                    {
                        int index = x + z * _size + y * _size * _size;
                        _world[x, y, z] = _buffer[index];
                    }
        }

        public void Dispose()
        {
            if (_buffer.IsCreated) _buffer.Dispose();
        }
    }
}