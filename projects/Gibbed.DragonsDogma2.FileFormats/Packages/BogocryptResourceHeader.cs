/* Copyright (c) 2025 Rick (rick 'at' gibbed 'dot' us)
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

namespace Gibbed.DragonsDogma2.FileFormats.Packages
{
    internal static class BogocryptResourceHeader
    {
        public static void Xor(Span<byte> span, Span<byte> table)
        {
            if (table == null || table.Length != 32)
            {
                throw new ArgumentException("xor table must be 32 bytes", nameof(table));
            }
            for (int i = 0; i < span.Length; i++)
            {
                int x = table[i % 32];
                x *= table[i % 29];
                x += i;
                span[i] ^= (byte)x;
            }
        }
    }
}
