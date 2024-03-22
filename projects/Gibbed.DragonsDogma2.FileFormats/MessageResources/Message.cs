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
using System.Collections.Generic;

namespace Gibbed.DragonsDogma2.FileFormats.MessageResources
{
    public class Message
    {
        private readonly Dictionary<uint, string> _Texts;
        private readonly List<object> _Arguments;

        public Message()
        {
            this._Texts = new();
            this._Arguments = new();
        }

        public Guid Guid { get; set; }
        public uint UnknownId { get; set; }
        public uint NameHash { get; set; }
        public string Name { get; set; }
        public Dictionary<uint, string> Texts => this._Texts;
        public List<object> Arguments => this._Arguments;

        public override string ToString()
        {
            // try to get English
            if (this.Texts.TryGetValue(1, out var text) == false)
            {
                // try to get Japanese
                if (this.Texts.TryGetValue(0, out text) == false)
                {

                    return $"{this.Name ?? this.Guid.ToString()}";
                }
            }
            return $"{this.Name ?? this.Guid.ToString()} = {text}";
        }
    }
}
