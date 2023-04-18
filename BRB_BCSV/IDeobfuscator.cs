﻿namespace BRB_BCSV
{
    public interface IDeobfuscator
    {
        long Initialize(NativeReader reader);
        bool AdjustPosition(NativeReader reader, long newPosition);
        void Deobfuscate(byte[] buffer, long position, int offset, int numBytes);
    }
}
