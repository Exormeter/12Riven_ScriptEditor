using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace Riven_Script_Editor
{
    static class Utility
    {
        public static string StringSingleSpace(string input)
        {
            return input.Replace("  ", " ");
        }

        public static string StringDoubleSpace(string input)
        {
            return input.Replace(" ", "  ");
        }

        public static byte[] StringEncode(string input)
        {
            string temp = input.Replace("ï", "∇");
            temp = temp.Replace("é", "≡");
            temp = temp.Replace("ö", "≒");
            var x = Encoding.GetEncoding("shift_jis").GetBytes(temp);

            for (int i = 0; i < x.Length - 1; i++)
                if (x[i] >= 0x80)
                {
                    i++;
                    if (x[i - 1] == 0x81 && x[i] == 0xe0) // ≒ -> ö
                    { x[i - 1] = 0x86; x[i] = 0x40; }
                    if (x[i - 1] == 0x81 && x[i] == 0xde) // ∇ -> ï
                    { x[i - 1] = 0x86; x[i] = 0x43; }
                    else if (x[i - 1] == 0x81 && x[i] == 0xdf) // ≡ -> é 
                    { x[i - 1] = 0x86; x[i] = 0x44; }
                    else if (x[i - 1] == 0x81 && x[i] == 0x61) // ∥ -> "I"
                    { x[i - 1] = 0x86; x[i] = 0x78; }
                    else if (x[i - 1] == 0x83 && x[i] == 0xB1) // Tau -> "t"
                    { x[i - 1] = 0x86; x[i] = 0xA4; }
                    else if (x[i - 1] == 0x83 && x[i] == 0xA5) // Eta -> "h"
                    { x[i - 1] = 0x86; x[i] = 0x98; }
                    else if (x[i - 1] == 0x83 && x[i] == 0x9F) // Alpha -> "a"
                    { x[i - 1] = 0x86; x[i] = 0x91; }
                    else if (x[i - 1] == 0x81 && x[i] == 0xAB) // ↓ -> "!"
                    { x[i - 1] = 0x86; x[i] = 0x50; }
                    //else if (x[i-1] == 0x81 && x[i] == 0x79) // 【 -> "「" 
                    //    { x[i-1] = 0x85; x[i] = 0xA0; }
                    //else if (x[i-1] == 0x81 && x[i] == 0x7A) // 】 -> "」"
                    //    { x[i-1] = 0x85; x[i] = 0xA1; }

                }

            return x;
        }

        public static string StringDecode(byte[] x)
        {
            for (int i = 0; i < x.Length - 1; i++)
                if (x[i] >= 0x80)
                {
                    i++;
                    if (x[i - 1] == 0x86 && x[i] == 0x40) // ö -> ≒
                    { x[i - 1] = 0x81; x[i] = 0xe0; }
                    else if (x[i - 1] == 0x86 && x[i] == 0x43) // ï -> ∇
                    { x[i - 1] = 0x81; x[i] = 0xde; }
                    else if (x[i - 1] == 0x86 && x[i] == 0x44) // é -> ≡
                    { x[i - 1] = 0x81; x[i] = 0xdf; }
                    else if (x[i - 1] == 0x86 && x[i] == 0x78) // "I" -> ∥
                    { x[i - 1] = 0x81; x[i] = 0x61; }
                    else if (x[i - 1] == 0x86 && x[i] == 0xA4) // "t" -> Tau
                    { x[i - 1] = 0x83; x[i] = 0xB1; }
                    else if (x[i - 1] == 0x86 && x[i] == 0x98) // "h" -> Eta
                    { x[i - 1] = 0x83; x[i] = 0xA5; }
                    else if (x[i - 1] == 0x86 && x[i] == 0x91) // "a" -> Alpha
                    { x[i - 1] = 0x83; x[i] = 0x9F; }
                    else if (x[i - 1] == 0x86 && x[i] == 0x50) // "!" -> ↓
                    { x[i - 1] = 0x81; x[i] = 0xAB; }
                    //else if (x[i-1] == 0x85 && x[i] == 0xA0) //  "「" -> 【
                    //    { x[i-1] = 0x81; x[i] = 0x79; }
                    //else if (x[i-1] == 0x85 && x[i] == 0xA1) // "」" -> 】
                    //    { x[i-1] = 0x81; x[i] = 0x7A; }
                }

            var output = Encoding.GetEncoding("shift-jis").GetString(x);
            output = output.Replace("∇", "ï");
            output = output.Replace("≡", "é");
            output = output.Replace("≒", "ö");

            return output;
        }

        public static string ToString(byte[] byteArray)
        {
            string byteCommandString = "";
            foreach (byte commandByte in byteArray)
            {
                byteCommandString += commandByte.ToString("X2") + " ";
            }
            return byteCommandString;
        }

        public static bool ToByteArray(string byteString, out byte[] byteArray)
        {
            List<byte> bytes = new List<byte>();
            Regex rgx = new Regex("^[A-Fa-f0-9\\s]*$");

            byteArray = bytes.ToArray();
            if (!rgx.IsMatch(byteString)) { return false; }

            List<string> byteStrings = byteString.Split(' ').ToList();

            byteStrings.RemoveAll(literal => literal.Equals(""));
            foreach (string byteChunk in byteStrings)
            {
                try
                {
                    bytes.Add(Convert.ToByte(int.Parse(byteChunk, System.Globalization.NumberStyles.HexNumber)));
                }
                catch(Exception e)
                {
                    return false;
                }
            }

            byteArray = bytes.ToArray();

            return true;
        }
    }
}