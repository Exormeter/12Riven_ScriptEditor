using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Riven_Script_Editor.Tokens
{
    class TokenSysMessage : Token
    {
        public new const TokenType Type = TokenType.sys_disp;


        private readonly MessagePointer sysPtr;

        public string SysMessage
        {
            get { return sysPtr.Message; }
            set { sysPtr.Message = value; }
        }

        public TokenSysMessage(DataWrapper wrapper, byte[] byteCommand, int pos, bool blank = false) : base(wrapper, byteCommand, pos)
        {
            sysPtr = new MessagePointer(3, 2, _byteCommand);
            SysMessage = _dataWrapper.ReadString(sysPtr.MsgPtrString);
            MessagePointerList.Add(sysPtr);
            _command = "Sys Message";
            _description = " Some kind of Sys message, not sure";
            UpdateData();
        }

        public override string GetMessages()
        {
            return SysMessage;
        }

        public override byte[] GetMessagesBytes()
        {
            byte[] msg = Utility.StringEncode(SysMessage);
            byte[] output = new byte[msg.Length + 1];
            msg.CopyTo(output, 0);

            return output;
        }

        public override void UpdateData()
        {
            Data2 = SysMessage;
        }

        public override void UpdateGui(MainWindow window)
        {
            base.UpdateGui(window);
            base.AddTextbox(window, "SysMessage", "SysMessage");
            base.AddSpacer(window);
        }
    }
}
