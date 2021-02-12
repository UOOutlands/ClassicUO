#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;

namespace ClassicUO.Utility
{
    internal static class HuesHelper
    {
        private static readonly byte[] _table = new byte[32]
        {
            0x00, 0x08, 0x10, 0x18, 0x20, 0x29, 0x31, 0x39, 0x41, 0x4A, 0x52, 0x5A, 0x62, 0x6A, 0x73, 0x7B, 0x83, 0x8B,
            0x94, 0x9C, 0xA4, 0xAC, 0xB4, 0xBD, 0xC5, 0xCD, 0xD5, 0xDE, 0xE6, 0xEE, 0xF6, 0xFF
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (byte, byte, byte, byte) GetBGRA(uint cl)
        {
            return ((byte)(cl & 0xFF),         // B
                    (byte)((cl >> 8) & 0xFF),  // G
                    (byte)((cl >> 16) & 0xFF), // R
                    (byte)((cl >> 24) & 0xFF)  // A
                );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint RgbaToArgb(uint rgba)
        {
            return (rgba >> 8) | (rgba << 24);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Color16To32(ushort c)
        {
            return (uint)(_table[(c >> 10) & 0x1F] | (_table[(c >> 5) & 0x1F] << 8) | (_table[c & 0x1F] << 16));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort Color32To16(uint c)
        {
            return (ushort)((((c & 0xFF) << 5) >> 8) | (((((c >> 16) & 0xFF) << 5) >> 8) << 10) | (((((c >> 8) & 0xFF) << 5) >> 8) << 5));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ConvertToGray(ushort c)
        {
            return (ushort)(((c & 0x1F) * 299 + ((c >> 5) & 0x1F) * 587 + ((c >> 10) & 0x1F) * 114) / 1000);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ColorToHue(Color c)
        {
            ushort origred = c.R;
            ushort origgreen = c.G;
            ushort origblue = c.B;
            const double scale = 31.0 / 255;
            ushort newred = (ushort)(origred * scale);

            if (newred == 0 && origred != 0)
            {
                newred = 1;
            }

            ushort newgreen = (ushort)(origgreen * scale);

            if (newgreen == 0 && origgreen != 0)
            {
                newgreen = 1;
            }

            ushort newblue = (ushort)(origblue * scale);

            if (newblue == 0 && origblue != 0)
            {
                newblue = 1;
            }

            ushort v = (ushort)((newred << 10) | (newgreen << 5) | newblue);

            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ColorNameToHue(string c)
        {
            switch (c)
            {
                case "red":
                    return Color32To16(0x0000FFFF);
                case "cyan":
                    return Color32To16(0xFFFF00FF);
                case "blue":
                    return Color32To16(0xFF0000FF);
                case "darkblue":
                    return Color32To16(0xA00000FF);
                case "lightblue":
                    return Color32To16(0xE6D8ADFF);
                case "purple":
                    return Color32To16(0x800080FF);
                case "yellow":
                    return Color32To16(0x00FFFFFF);
                case "lime":
                    return Color32To16(0x00FF00FF);
                case "magenta":
                    return Color32To16(0xFF00FFFF);
                case "white":
                    return Color32To16(0xFFFEFEFF);
                case "silver":
                    return Color32To16(0xC0C0C0FF);
                case "gray":
                    return Color32To16(0x808080FF);
                case "grey":
                    return Color32To16(0x808080FF);
                case "black":
                    return Color32To16(0x010101FF);
                case "orange":
                    return Color32To16(0x00A5FFFF);
                case "brown":
                    return Color32To16(0x2A2AA5FF);
                case "maroon":
                    return Color32To16(0x000080FF);
                case "green":
                    return Color32To16(0x008000FF);
                case "olive":
                    return Color32To16(0x008080FF);
                default:
                    return 65535;
            }
        }
    }
}