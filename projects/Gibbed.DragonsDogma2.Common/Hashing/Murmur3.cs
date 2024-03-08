/* Copyright (c) 2024 Rick (rick 'at' gibbed 'dot' us)
 *
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 *
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 *
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using System;

namespace Gibbed.DragonsDogma2.Common.Hashing
{
    public static class Murmur3
    {
        private const uint _C1 = 0xCC9E2D51u;
        private const uint _C2 = 0x1B873593u;

        public static uint Compute(byte[] buffer, int offset, int count, uint seed)
        {
            ReadOnlySpan<byte> span = new(buffer, offset, count);
            return Compute(span, seed);
        }

        public static uint Compute(ReadOnlySpan<byte> span, uint seed)
        {
            uint hash = seed;

            int offset = 0;

            // https://github.com/aappleby/smhasher/blob/92cf3702fcfaadc84eb7bef59825a23e0cd84f56/src/MurmurHash3.cpp#L110
            int headCount = span.Length / 4;
            for (int block = 0; block < headCount; block++, offset += 4)
            {
                var value = ((uint)span[offset + 0] << 0) |
                    ((uint)span[offset + 1] << 8) |
                    ((uint)span[offset + 2] << 16) |
                    ((uint)span[offset + 3] << 24);
                value *= _C1;
                value = (value >> 17) | (value << 15);
                value *= _C2;
                hash ^= value;
                hash = (hash >> 19) | (hash << 13);
                hash = (hash * 5) + 0xE6546B64u;
            }

            // https://github.com/aappleby/smhasher/blob/92cf3702fcfaadc84eb7bef59825a23e0cd84f56/src/MurmurHash3.cpp#L126
            int tailCount = span.Length % 4;
            if (tailCount > 0)
            {
                uint value = tailCount switch
                {
                    3 => ((uint)span[offset + 0] << 0) |
                        ((uint)span[offset + 1] << 8) |
                        ((uint)span[offset + 2] << 16),
                    2 => ((uint)span[offset + 0] << 0) |
                        ((uint)span[offset + 1] << 8),
                    1 => (uint)span[offset + 0] << 0,
                    _ => throw new InvalidOperationException(),
                };
                value *= _C1;
                value = (value >> 17) | (value << 15);
                value *= _C2;
                hash ^= value;
            }

            // https://github.com/aappleby/smhasher/blob/92cf3702fcfaadc84eb7bef59825a23e0cd84f56/src/MurmurHash3.cpp#L70
            hash ^= (uint)span.Length;
            hash ^= hash >> 16;
            hash *= 0x85EBCA6Bu;
            hash ^= hash >> 13;
            hash *= 0xC2B2AE35u;
            hash ^= hash >> 16;
            return hash;
        }
    }
}
