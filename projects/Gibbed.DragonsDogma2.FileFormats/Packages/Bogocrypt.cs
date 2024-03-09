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
    internal struct Bogocrypt
    {
        private static readonly BigInteger _Modulus;
        private static readonly BigInteger _Exponent;

        static Bogocrypt()
        {
            _Modulus = new(new byte[]
            {
                0x7D, 0x0B, 0xF8, 0xC1, 0x7C, 0x23, 0xFD, 0x3B,
                0xD4, 0x75, 0x16, 0xD2, 0x33, 0x21, 0xD8, 0x10,
                0x71, 0xF9, 0x7C, 0xD1, 0x34, 0x93, 0xBA, 0x77,
                0x26, 0xFC, 0xAB, 0x2C, 0xEE, 0xDA, 0xD9, 0x1C,
                0x89, 0xE7, 0x29, 0x7B, 0xDD, 0x8A, 0xAE, 0x50,
                0x39, 0xB6, 0x01, 0x6D, 0x21, 0x89, 0x5D, 0xA5,
                0xA1, 0x3E, 0xA2, 0xC0, 0x8C, 0x93, 0x13, 0x36,
                0x65, 0xEB, 0xE8, 0xDF, 0x06, 0x17, 0x67, 0x96,
                0x06, 0x2B, 0xAC, 0x23, 0xED, 0x8C, 0xB7, 0x8B,
                0x90, 0xAD, 0xEA, 0x71, 0xC4, 0x40, 0x44, 0x9D,
                0x1C, 0x7B, 0xBA, 0xC4, 0xB6, 0x2D, 0xD6, 0xD2,
                0x4B, 0x62, 0xD6, 0x26, 0xFC, 0x74, 0x20, 0x07,
                0xEC, 0xE3, 0x59, 0x9A, 0xE6, 0xAF, 0xB9, 0xA8,
                0x35, 0x8B, 0xE0, 0xE8, 0xD3, 0xCD, 0x45, 0x65,
                0xB0, 0x91, 0xC4, 0x95, 0x1B, 0xF3, 0x23, 0x1E,
                0xC6, 0x71, 0xCF, 0x3E, 0x35, 0x2D, 0x6B, 0xE3, 
                0x00,
            });
            _Exponent = new(new byte[]
            {
                0x01, 0x00, 0x01, 0x00,
            });
        }

        private byte[] _XorBytes;

        public static Bogocrypt Create(ReadOnlySpan<byte> key)
        {
            if (key.Length != 128)
            {
                throw new ArgumentOutOfRangeException(nameof(key));
            }

            var keyPaddedBytes = key.ToArray();
            Array.Resize(ref keyPaddedBytes, key.Length + 1);

            BigInteger keyValue = new(keyPaddedBytes);
            BigInteger xor = BigInteger.ModPow(keyValue, _Exponent, _Modulus);

            Bogocrypt instance;
            instance._XorBytes = xor.ToByteArray();
            return instance;
        }

        public readonly void Xor(Span<byte> span)
        {
            var xorBytes = this._XorBytes;
            for (int i = 0; i < span.Length; i++)
            {
                int x = xorBytes[i % 32];
                x *= xorBytes[i % 29];
                x += i;
                span[i] ^= (byte)x;
            }
        }
    }
}
