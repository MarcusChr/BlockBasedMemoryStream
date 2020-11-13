using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace com.marcuslc.BlockBasedMemoryStream
{
    public unsafe class Node
    {
        public Node(int bufferSize)
        {
            Next = null;

            Value = new ValueHolder
            {
                start = 0,
                pointer = Marshal.AllocHGlobal(bufferSize).ToPointer(),
                end = 0
            };
        }

        public Node Next;
        public ValueHolder Value;

        ~Node()
        {
            Marshal.FreeHGlobal(new IntPtr(Value.pointer));
        }
    }

    public unsafe struct ValueHolder
    {
        public int start;
        public void* pointer;
        public int end;
    }
}
