/*
 * Issues:
 * Automatically create archive
 *  Randomly spits out gibberish then dies
 *  Text on choices is misaligned severely
 *  I think the way I made TokenIf create an IntGoto token is messing things up
 *      Nope, something else is causing it. Look for commands that happen before it screws up.
 */


using System;

namespace Riven_Script_Editor.Tokens
{

    public class DataWrapper
    {

        private readonly byte[] data;

        public byte[] RawArray
        {
            get { return data; }
        }
        public byte this[int i]
        {
            get => data[i];
            set => data[i] = value;
        }

        public int pos = 0;
        public DataWrapper(byte[] binData)
        {
            data = binData;
        }


        public byte ReadUInt8(int offset, bool from_current_pos = true)
        {
            if (from_current_pos)
                return data[pos + offset];

            return data[offset];
        }

        public UInt16 ReadUInt16(int offset, bool from_current_pos = true)
        {
            if (from_current_pos)
                return (UInt16)(data[pos + offset] + (data[pos + offset + 1] << 8));

            return (UInt16)(data[offset] + (data[offset + 1] << 8));
        }

        public Int32 ReadUInt32(int offset)
        {
            return data[pos + offset] + (data[pos + offset + 1] << 8) + (data[pos + offset + 2] << 16) + (data[pos + offset + 3] << 24);
        }

        public String ReadString(int offset)
        {
            int count = 0;

            // Read until null character
            while (true)
            {
                byte c = data[offset + count];
                if (c == 0) break;
                count++;
            }

            byte[] buffer = new byte[count];
            Array.Copy(data, offset, buffer, 0, count);

            // This is a Shift-JIS string, decode it
            return Utility.StringDecode(buffer);
        }
    }
}