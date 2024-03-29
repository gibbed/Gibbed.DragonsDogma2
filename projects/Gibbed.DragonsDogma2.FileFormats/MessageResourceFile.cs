﻿/* Copyright (c) 2024 Rick (rick 'at' gibbed 'dot' us)
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
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using Gibbed.DragonsDogma2.Common;
using Gibbed.DragonsDogma2.FileFormats.MessageResources;
using Gibbed.Memory;

namespace Gibbed.DragonsDogma2.FileFormats
{
    // via.MessageResource
    public class MessageResourceFile
    {
        private readonly List<Argument> _Arguments;
        private readonly List<Message> _Messages;

        public MessageResourceFile()
        {
            this._Arguments = new();
            this._Messages = new();
        }

        public Endian Endian { get; set; }
        public List<Argument> Arguments => this._Arguments;
        public List<Message> Messages => this._Messages;

        public void Serialize(IBufferWriter<byte> writer)
        {
            throw new NotImplementedException();
        }

        public void Deserialize(ReadOnlySpan<byte> span)
        {
            int index = 0;
            var header = FileHeader.Read(span, ref index);
            var endian = header.Endian;
            var encoding = endian == Endian.Little ? Encoding.Unicode : Encoding.BigEndianUnicode;

            index = header.DataOffset;
            var dataHeader = DataHeader.Read(span, ref index, endian);
            var messageOffsets = new int[dataHeader.MessageCount];
            for (int i = 0; i < dataHeader.MessageCount; i++)
            {
                messageOffsets[i] = span.ReadValueOffset32(ref index, endian);
            }

            // TODO(gibbed): string data is assumed to be at the end of the file
            if (dataHeader.LanguageTableOffset > dataHeader.StringsOffset ||
                dataHeader.ArgumentTypeTableOffset > dataHeader.StringsOffset ||
                dataHeader.ArgumentNameTableOffset > dataHeader.StringsOffset)
            {
                throw new FormatException();
            }

            byte[] stringBuffer;
            {
                // TODO(gibbed): this assumes the string data is always at the end of the file?
                int stringsSize = span.Length - dataHeader.StringsOffset;
                stringBuffer = new byte[stringsSize];
                span.Slice(dataHeader.StringsOffset).CopyTo(stringBuffer);
                byte previousByte = 0;
                for (int i = 0; i < stringBuffer.Length; i++)
                {
                    var b = stringBuffer[i];
                    var c = previousByte;
                    c ^= b;
                    c ^= XorTable[i & 0xF];
                    stringBuffer[i] = c;
                    previousByte = b;
                }
            }
            Dictionary<int, string> stringCache = new();
            string ReadString(ReadOnlySpan<byte> span, int offset)
            {
                if (offset < dataHeader.StringsOffset ||
                    offset > dataHeader.StringsOffset + stringBuffer.Length)
                {
                    throw new FormatException();
                }
                offset -= dataHeader.StringsOffset;
                if (stringCache.TryGetValue(offset, out var s) == false)
                {
                    s = stringCache[offset] = span.ReadStringZ(ref offset, encoding);
                }
                return s;
            }

            var languageIds = new uint[dataHeader.LanguageCount];
            if (dataHeader.LanguageCount > 0)
            {
                index = dataHeader.LanguageTableOffset;
                for (int i = 0; i < dataHeader.LanguageCount; i++)
                {
                    languageIds[i] = span.ReadValueU32(ref index, endian);
                }
            }

            var arguments = new Argument[dataHeader.ArgumentCount];
            if (dataHeader.ArgumentCount > 0)
            {
                index = dataHeader.ArgumentNameTableOffset;
                var argumentNameOffsets = new int[dataHeader.ArgumentCount];
                for (int i = 0; i < dataHeader.ArgumentCount; i++)
                {
                    argumentNameOffsets[i] = span.ReadValueOffset32(ref index, endian);
                }

                index = dataHeader.ArgumentTypeTableOffset;
                for (int i = 0; i < dataHeader.ArgumentCount; i++)
                {
                    var argumentType = (ArgumentType)span.ReadValueS32(ref index, endian);
                    var argumentName = ReadString(stringBuffer, argumentNameOffsets[i]);
                    arguments[i] = new()
                    {
                        Type = argumentType,
                        Name = argumentName,
                    };
                }
            }

            var messageHeaders = new MessageHeader[dataHeader.MessageCount];
            var messageTextOffsets = new int[dataHeader.MessageCount, dataHeader.LanguageCount];
            for (int i = 0; i < dataHeader.MessageCount; i++)
            {
                index = messageOffsets[i];
                messageHeaders[i] = MessageHeader.Read(span, ref index, endian);
                for (int j = 0; j < dataHeader.LanguageCount; j++)
                {
                    messageTextOffsets[i, j] = span.ReadValueOffset32(ref index, endian);
                }
            }

            var messages = new Message[dataHeader.MessageCount];
            for (int i = 0; i < dataHeader.MessageCount; i++)
            {
                var messageHeader = messageHeaders[i];
                Message message = new()
                {
                    Guid = messageHeader.Guid,
                    UnknownId = messageHeader.UnknownId,
                    NameHash = messageHeader.NameHash,
                    Name = ReadString(stringBuffer, messageHeader.NameOffset),
                };
                for (int j = 0; j < dataHeader.LanguageCount; j++)
                {
                    var languageId = languageIds[j];
                    message.Texts.Add(languageId, ReadString(stringBuffer, messageTextOffsets[i, j]));
                }
                index = messageHeader.ArgumentTableOffset;
                for (int j = 0; j < dataHeader.ArgumentCount; j++)
                {
                    object argumentValue;
                    switch (arguments[j].Type)
                    {
                        case ArgumentType.Int:
                        {
                            argumentValue = span.ReadValueS32(ref index, endian);
                            index += 4;
                            break;
                        }
                        case ArgumentType.Float:
                        {
                            argumentValue = span.ReadValueF64(ref index, endian);
                            break;
                        }
                        case ArgumentType.None:
                        case ArgumentType.String:
                        {
                            var argumentStringOffset = span.ReadValueS32(ref index, endian);
                            index += 4;
                            argumentValue = ReadString(stringBuffer, argumentStringOffset);
                            break;
                        }
                        default:
                        {
                            throw new NotSupportedException();
                        }
                    }
                    message.Arguments.Add(argumentValue);
                }
                messages[i] = message;
            }

            this._Arguments.Clear();
            this._Messages.Clear();
            this.Endian = endian;
            this.Arguments.AddRange(arguments);
            this.Messages.AddRange(messages);
        }

        private static readonly byte[] XorTable;

        static MessageResourceFile()
        {
            XorTable = new byte[]
            {
                0xCF, 0xCE, 0xFB, 0xF8, 0xEC, 0x0A, 0x33, 0x66,
                0x93, 0xA9, 0x1D, 0x93, 0x50, 0x39, 0x5F, 0x09,
            };
        }
    }
}
