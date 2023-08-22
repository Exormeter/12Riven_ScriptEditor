using System;
using System.Collections.Generic;
using System.Text;

namespace Riven_Script_Editor.Tokens
{
    class TokenSelectDisp0x74 : Token
    {
        
        byte num_entries;
        UInt16 fixed1;
        public List<SelectDispEntry> Entries = new List<SelectDispEntry>();

        public TokenSelectDisp0x74(DataWrapper wrapper, int pos): base(wrapper, pos)
        {
            _length += _byteCommand[1] * 8;
            _byteCommand = wrapper.Slice(pos, _length);
            Data = Utility.ToString(_byteCommand);

            _description = "Display choices [@TODO: Fix jump/label handling]";
            num_entries = _byteCommand[1];

            //fixed1 = 0x6009;

            for (int i = 0; i < num_entries; i++)
            {
                var entry = new SelectDispEntry();
                entry.MsgPtr = _dataWrapper.ReadUInt16(pos + 6 + (i * 8));
                entry.JumpAddress = _dataWrapper.ReadUInt16(i * 8 + 6);
                entry.Unknown = _dataWrapper.ReadUInt16(i * 8 + 8);
                entry.ChoiceId = _dataWrapper.ReadUInt16(i * 8 + 10);

                entry.Message = _dataWrapper.ReadString(entry.MsgPtr);
                Entries.Add(entry);
            }

            UpdateData();
        } 
        public override string GetMessages()
        {
            string msg = "";
            foreach (var e in Entries)
                msg += e.Message + " ";

            return msg;
        }

        public override byte[] GetMessagesBytes()
        {
            byte[] msg;
            List<byte[]> messages = new List<byte[]>();
            int len = 0;
            int offset = 0;

            foreach (var e in Entries)
            {
                msg = Utility.StringEncode(e.Message);
                messages.Add(msg);
                len += msg.Length + 1;
            }

            byte[] output = new byte[len];
            foreach (var m in messages)
            {
                m.CopyTo(output, offset);
                offset += m.Length + 1;
            }

            return output;
        }

        public override int SetMessagePointer(int offset)
        {
            for (int i = 0; i < Entries.Count; i++)
            {
                var e = Entries[i];
                e.MsgPtr = (UInt16)offset;
                offset += Utility.StringEncode(e.Message).Length + 1;
            }
            return offset;
        }

        public override void UpdateData()
        {
            Data = "Choices: " + Entries.Count.ToString();

            for (int i = 0; i < Entries.Count; i++)
            {
                if (i == 0)
                    Data2 += Entries[i].Message;
                else
                    Data2 += " / " + Entries[i].Message;
            }
        }

        public override void UpdateGui(MainWindow window)
        {
            base.UpdateGui(window);
            PopulateEntryList(window, Entries, (sender, ev) =>
            {
                if (ev.AddedItems.Count == 0 || !(ev.AddedItems[0] is SelectDispEntry))
                    return;

                base.UpdateGui(window, false);
                var e = (SelectDispEntry)ev.AddedItems[0];
                AddTextbox(window, "Message", "Message", e);
                //AddUint16(window, "JumpAddress", "Unknown1", e);
                //AddUint16(window, "Unknown", "Unknown", e);
                //AddUint16(window, "Choice ID", "ChoiceId", e);
            });
        }
    }

    class SelectDispEntry
    {
        public UInt16 MsgPtr { get; set; }
        public String Message { get; set; }
        public UInt16 JumpAddress { get; set; } //Address to go to if selected. [FF FF] seems to mean don't jump.
        public UInt16 Unknown { get; set; } //[01 00] fixed
        public UInt16 ChoiceId { get; set; }
    }
}
