
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
    }
}
