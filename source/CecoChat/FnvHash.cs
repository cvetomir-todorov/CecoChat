﻿namespace CecoChat
{
    // TODO: consider implementing via HashCode.Combine() https://github.com/Cyan4973/xxHash
    public interface INonCryptoHash
    {
        int Compute(long value);
    }

    public sealed class FnvHash : INonCryptoHash
    {
        public int Compute(long value)
        {
            short value0 = (short)(value >> 48);
            short value1 = (short)(value >> 32);
            short value2 = (short)(value >> 16);
            short value3 = (short) value;

            int hash = 92821;
            const int prime = 486187739;

            unchecked // overflow is fine
            {
                hash = hash * prime ^ value0;
                hash = hash * prime ^ value1;
                hash = hash * prime ^ value2;
                hash = hash * prime ^ value3;
            }

            return hash;
        }
    }
}
