namespace BlockBasedMemoryStream
{
    internal unsafe struct ValueBlock
    {
        public int start;
        public void* pointer;
        public int end;
    }
}