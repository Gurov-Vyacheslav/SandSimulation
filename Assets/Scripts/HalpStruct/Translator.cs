
namespace SandSimulation.HalpStruct
{
    internal static class Translator
    {
        public static int ToIndex(int x, int y, int z, int size)
        {
            return x + y * size + z * size * size;
        }
        public static void ToXYZ(int index, int size, out int x, out int y, out int z)
        {
            z = index / (size * size);
            y = (index / size) % size;
            x = index % size;
        }

        //public static int ToIndex(int x, int y, int z, int size)
        //{
        //    int chunkXCount = 2;
        //    int chunkZCount = 4;

        //    int chunkSizeX = size / chunkXCount;
        //    int chunkSizeZ = size / chunkZCount;

        //    int chunkIndex = (z / chunkSizeZ) * chunkXCount + (x / chunkSizeX);
        //    int chunkCapacity = size * size * size / 8;

        //    return chunkIndex * chunkCapacity +
        //           y * (chunkSizeX * chunkSizeZ) +
        //           (z % chunkSizeZ) * chunkSizeX +
        //           (x % chunkSizeX);
        //}

        //public static void ToXYZ(int index, int size, out int x, out int y, out int z)
        //{
        //    int chunkXCount = 2;
        //    int chunkZCount = 4;

        //    int chunkSizeX = size / chunkXCount;
        //    int chunkSizeZ = size / chunkZCount;
        //    int chunkCapacity = size * size * size / 8;

        //    int chunkIndex = index / chunkCapacity;
        //    int indexInChunk = index % chunkCapacity;

        //    x = (chunkIndex % chunkXCount) * chunkSizeX + indexInChunk % chunkSizeX;
        //    z = (chunkIndex / chunkXCount) * chunkSizeZ + (indexInChunk / chunkSizeX) % chunkSizeZ;
        //    y = indexInChunk / (chunkSizeX * chunkSizeZ);
        //}
    }
}
