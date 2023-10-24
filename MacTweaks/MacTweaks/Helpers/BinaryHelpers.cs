using System;

namespace MacTweaks.Helpers
{
    public static class BinaryHelpers
    {
        public static void PrintBinary(this byte input)
        {
            Console.WriteLine(Convert.ToString(input, 2).PadLeft(8, '0'));
        }
    
        public static void PrintBinary(this sbyte input)
        {
            unchecked((byte) input).PrintBinary();
        }
    
        public static void PrintBinary(this short input)
        {
            Console.WriteLine(Convert.ToString(input, 2).PadLeft(16, '0'));
        }
    
        public static void PrintBinary(this ushort input)
        {
            unchecked((short) input).PrintBinary();
        }
    
        public static void PrintBinary(this int input)
        {
            Console.WriteLine(Convert.ToString(input, 2).PadLeft(32, '0'));
        }
    
        public static void PrintBinary(this uint input)
        {
            unchecked((int) input).PrintBinary();
        }
    
        public static void PrintBinary(this long input)
        {
            Console.WriteLine(Convert.ToString(input, 2).PadLeft(64, '0'));
        }
    
        public static void PrintBinary(this ulong input)
        {
            unchecked((long) input).PrintBinary();
        }

        public static unsafe void PrintBinary<T>(this T input) where T: unmanaged, Enum
        {
            if (sizeof(T) == 1)
            {
                PrintBinary((byte) (object) input);
            }
            
            if (sizeof(T) == 2)
            {
                PrintBinary((short) (object) input);
            }
            
            if (sizeof(T) == 4)
            {
                PrintBinary((int) (object) input);
            }
            
            if (sizeof(T) == 8)
            {
                PrintBinary((ulong) (object) input);
            }
        }
    }
}