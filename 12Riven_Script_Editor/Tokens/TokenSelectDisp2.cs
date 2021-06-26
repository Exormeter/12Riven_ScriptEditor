using System;
using System.Collections.Generic;
using System.Text;

namespace Riven_Script_Editor.Tokens
{
    class TokenSelectDisp2 : Token
    {

        public new const TokenType Type = TokenType.sel_disp2;
       
        public List<SelectDisp2Entry> Entries = new List<SelectDisp2Entry>();

        public TokenSelectDisp2(DataWrapper wrapper, byte[] byteCommand, int pos, bool blank=false): base(wrapper, byteCommand, pos)
        {
            _command = "Select Disp2";
            _description = "Display choices (version 2)";


            for (int i = 8; i < byteCommand.Length; i += 8)
            {
                var entry = new SelectDisp2Entry();
                //entry.MsgPtr = BitConverter.ToUInt16(byteCommand, i);
                MessagePointerList.Add(new MessagePointer(i + 1, i, _byteCommand));
                entry.choisePointer = MessagePointerList[MessagePointerList.Count - 1];
                entry.Message = _dataWrapper.ReadString(MessagePointerList[MessagePointerList.Count - 1].MsgPtrString);
                // Remove double spaces
                //entry.TempMsg = Utility.StringSingleSpace(entry.Message);
                Entries.Add(entry);
            }
            
            
            UpdateData();
        }

        public override string GetMessages()
        {
            string msg = "";
            foreach (var e in Entries)
                msg += e.Message + "/";

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
      

        public override void UpdateGui(MainWindow window)
        {
            base.UpdateGui(window);
            PopulateEntryList(window, Entries, (sender, ev) => {
                if (ev.AddedItems.Count == 0 || !(ev.AddedItems[0] is SelectDisp2Entry))
                    return;

                base.UpdateGui(window, false);
                var e = (SelectDisp2Entry)ev.AddedItems[0];
                AddTextbox(window, "Choise", "Message", e);
            });
        }
    }

    class SelectDisp2Entry
    {
        public MessagePointer choisePointer;
        public String Message {
            get { return choisePointer.Message; }
            set { choisePointer.Message = value; }
        }

        public byte[] GetMessagesBytes()
        {
            byte[] msg = Utility.StringEncode(Message);
            byte[] output = new byte[msg.Length + 1];
            msg.CopyTo(output, 0);

            return output;
        }
    }
}
