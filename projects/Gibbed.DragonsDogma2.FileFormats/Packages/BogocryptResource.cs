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
using System.Numerics;

namespace Gibbed.DragonsDogma2.FileFormats.Packages
{
    public struct BogocryptResource
    {
        private static readonly BigInteger _Modulus;
        private static readonly BigInteger _Exponent;

        static BogocryptResource()
        {
            // Currently Type1, Type2, Type3, and Type4 all share the same modulus/exponent
            _Modulus = new(new byte[]
            {
                0x13, 0xD7, 0x9C, 0x89, 0x88, 0x91, 0x48, 0x10,
                0xD7, 0xAA, 0x78, 0xAE, 0xF8, 0x59, 0xDF, 0x7D,
                0x3C, 0x43, 0xA0, 0xD0, 0xBB, 0x36, 0x77, 0xB5,
                0xF0, 0x5C, 0x02, 0xAF, 0x65, 0xD8, 0x77, 0x03,
                0x00,
            });
            _Exponent = new(new byte[]
            {
                0xC0, 0xC2, 0x77, 0x1F, 0x5B, 0x34, 0x6A, 0x01,
                0xC7, 0xD4, 0xD7, 0x85, 0x2E, 0x42, 0x2B, 0x3B,
                0x16, 0x3A, 0x17, 0x13, 0x16, 0xEA, 0x83, 0x30,
                0x30, 0xDF, 0x3F, 0xF4, 0x25, 0x93, 0x20, 0x01,
            });
        }

        public static byte[] Decrypt(ReadOnlySpan<byte> span, long size)
        {
            var result = new byte[size];

            var paddedBytes = new byte[64 + 1];

            int spanIndex;
            long offset;

            void DecryptBlock(ReadOnlySpan<byte> span, int blockSize)
            {
                span.Slice(spanIndex + 0, 64).CopyTo(paddedBytes);
                BigInteger key = new(paddedBytes);
                span.Slice(spanIndex + 64, 64).CopyTo(paddedBytes);
                BigInteger value = new(paddedBytes);

                BigInteger divisor = BigInteger.ModPow(key, _Exponent, _Modulus);
                var block = BigInteger.Divide(value, divisor);
                var blockBytes = block.ToByteArray();

                Array.Copy(blockBytes, 0, result, offset, blockSize);
            }

            for (spanIndex = 0, offset = 0; offset + 8 <= size; offset += 8, spanIndex += 128)
            {
                DecryptBlock(span, 8);
            }

            var tailSize = (int)(size & 7);
            if (tailSize > 0)
            {
                DecryptBlock(span, tailSize);
            }

            return result;
        }
    }
}
