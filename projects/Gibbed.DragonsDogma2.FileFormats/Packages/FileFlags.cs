﻿/* Copyright (c) 2025 Rick (rick 'at' gibbed 'dot' us)
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
    [Flags]
    public enum FileFlags : ushort
    {
        None = 0,

        Extended = 1 << 0,
        Blocks = 1 << 1,
        Version = 1 << 2,
        Signed = 1 << 3,
        Unknown4 = 1 << 4, // TODO(gibbed): Monster Hunter: Wilds

        // flags that don't appear to exist yet, for completion
        Unknown5 = 1 << 5,
        Unknown6 = 1 << 6,
        Unknown7 = 1 << 7,
        Unknown8 = 1 << 8,
        Unknown9 = 1 << 9,
        Unknown10 = 1 << 10,
        Unknown11 = 1 << 11,
        Unknown12 = 1 << 12,
        Unknown13 = 1 << 13,
        Unknown14 = 1 << 14,
        Unknown15 = 1 << 15,

        Known = Extended | Blocks | Version | Signed | Unknown4,
    }
}
