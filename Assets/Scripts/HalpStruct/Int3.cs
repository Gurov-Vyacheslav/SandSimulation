namespace SandSimulation.HalpStruct
{
    internal struct Int3 
    { 
        public int x, y, z; 
        public Int3(int X, int Y, int Z) 
        { 
            x = X; 
            y = Y; 
            z = Z; 
        }

        public static readonly Int3[] Directions = new Int3[]
        {
            new Int3(0, -1, 0),    // вниз
            new Int3(-1, -1, 0),   // влево вниз
            new Int3(1, -1, 0),    // вправо вниз
            new Int3(0, -1, -1),   // назад вниз
            new Int3(0, -1, 1)     // вперед вниз
        };
    }
}
