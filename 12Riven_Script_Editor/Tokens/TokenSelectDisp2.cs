using System;
using System.Collections.Generic;
using System.Text;

namespace Riven_Script_Editor.Tokens
{
    class TokenSelectDisp2 : Token
    {
        public new const TokenType Type = TokenType.sel_disp2;
        
        int num_entries;
        UInt32 fixed1;
        public List<SelectDisp2Entry> Entries = new List<SelectDisp2Entry>();
        private List<string> _identical_jp_labels = new List<string>();
        public List<string> IdenticalJpLabels
        {
            get => _identical_jp_labels;
            set => _identical_jp_labels = value;
        }

        public TokenSelectDisp2(DataWrapper wrapper, byte[] byteCommand, int pos, bool blank=false): base(wrapper, byteCommand, pos)
        {
            _command = "Select Disp2";
            _description = "Display choices (version 2)";


            for (int i = 8; i < byteCommand.Length; i += 8)
            {
                var entry = new SelectDisp2Entry();
                entry.MsgPtr = BitConverter.ToUInt16(byteCommand, i);
                entry.Unknown1 = 999;
                entry.Unknown2 = 999;
                entry.ChoiceId = 999;

                entry.Message = _dataWrapper.ReadString(entry.MsgPtr);
                // Remove double spaces
                entry.TempMsg = Utility.StringSingleSpace(entry.Message);
                Entries.Add(entry);
            }
            
            
            UpdateData();
        }

        public void UpdateEntryMsgPointer(int entryIndex, UInt16 msgPtr)
        {
            entryIndex += 1;
            _byteCommand[(entryIndex * 8) + 1] = BitConverter.GetBytes(msgPtr)[1];
            _byteCommand[(entryIndex * 8)] = BitConverter.GetBytes(msgPtr)[0];
            Entries[entryIndex - 1].MsgPtr = msgPtr;
        }

        public override string GetMessages()
        {
            string msg = "";
            foreach (var e in Entries)
                msg += e.Message + "/" + e.MessageJp + "/";

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
            for (int i=0; i<Entries.Count; i++)
            {
                var e = Entries[i];
                e.MsgPtr = (UInt16)offset;
                offset += Utility.StringEncode(e.Message).Length + 1;
            }
            return offset;
        }

        public override void UpdateData()
        {
            //_length = 6 + 8 * Entries.Count;
            num_entries = (byte)Entries.Count;
            Data = "Choices: " + Entries.Count.ToString();

            var m = new List<string>();
            for (int i = 0; i < Entries.Count; i++)
            {
                Entries[i].Message = Utility.StringDoubleSpace(Entries[i].TempMsg);
                m.Add(Entries[i].Message);
            }

            Data2 = String.Join(" / ", m);
        }

        public override void UpdateGui(MainWindow window)
        {
            base.UpdateGui(window);
            PopulateEntryList(window, Entries, (sender, ev) => {
                if (ev.AddedItems.Count == 0 || !(ev.AddedItems[0] is SelectDisp2Entry))
                    return;

                base.UpdateGui(window, false);
                var e = (SelectDisp2Entry)ev.AddedItems[0];
                AddTextbox(window, "Choise", "TempMsg", e);
                //AddTextbox("Choise Message", "MessageJp", e);
                //AddUint16("Unknown1", "Unknown1", e);
                //AddUint16("Unknown2", "Unknown2", e);
                //AddUint16("Choice ID", "ChoiceId", e);
            });
        }
    }

    class SelectDisp2Entry
    {
        public UInt16 MsgPtr { get; set; }
        public String Message { get; set; }
        public String MessageJp { get; set; }
        public String TempMsg { get; set; }
        public UInt16 Unknown1 { get; set; } //[FF FF] fixed
        public UInt16 Unknown2 { get; set; } //[01 00] fixed
        public UInt16 ChoiceId { get; set; }

        public byte[] GetMessagesBytes()
        {
            byte[] msg = Utility.StringEncode(Message);
            byte[] output = new byte[msg.Length + 1];
            msg.CopyTo(output, 0);

            return output;
        }
    }
}
