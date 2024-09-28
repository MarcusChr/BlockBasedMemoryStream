using System;
using System.Runtime.InteropServices;

namespace BlockBasedMemoryStream
{
    internal unsafe class Node
    {
        public Node Next;
        public ValueBlock Value;

        public Node(int bufferSize)
        {
            Next = null;
            Value = new ValueBlock
            {
                start = 0,
                pointer = Marshal.AllocHGlobal(bufferSize).ToPointer(),
                end = 0
            };
        }


        ~Node()
        {
            Marshal.FreeHGlobal(new IntPtr(Value.pointer));
        }
    }
}
