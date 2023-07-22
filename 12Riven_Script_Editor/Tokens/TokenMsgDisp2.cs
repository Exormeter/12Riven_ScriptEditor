using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Riven_Script_Editor.Tokens
{
    class TokenMsgDisp2 : Token
    {
        public new const TokenType Type = TokenType.message;

        public MessagePointer MsgPtr;

        public string CompleteMessage 
        {
            get { return MsgPtr.Message; } 
            set { MsgPtr.Message = value; } 
        }
        public string Speaker { get; set; }
        public string Message { get; set; }
        public string MessageEnding { get; set; }

        public string MessageJp { get; set; }

        static Regex terminator_regex = new Regex(@"(%[%ABCDEKNPpSTV0-9]*?)$");

        public TokenMsgDisp2(DataWrapper wrapper, byte[] byteCommand, int pos, bool blank = false) : base(wrapper, byteCommand, pos)
        {
            
            init();
        }

        public TokenMsgDisp2(TokenMsgDisp2 token) : base((Token)token)
        {
            init();
        }

        private void init()
        {
            Splitable = "No";
            MsgPtr = new MessagePointer(7, 6, _byteCommand);
            _description = "Displays text spoken by a character.\n" +
            "\nSpeaker ID: The sprite to animate" +
            "\n\nColor:\n%C<RGBA>\n(eg. %C8CFF would give cyan, %CF66A is a slightly transparent red. Return to white using %CFFFF)" +
            "\n\nFade in:\n%FS<Text>%FE" +
            "\n\nWait for user input:\n%K" +
            "\n\nAlignment:\n%LR (right-align)\n%LC (center)" +
            "\n\nNew line:\n%N" +
            "\n\nFade out text:\n%O (used in the \"Infinity Loop\" Message)" +
            "\n\nClear text:\n%P (usually at end of dialogue)" +
            "\n\nTime delay?:\n%T##\n(eg. %T60 = delay of 60 ticks)" +
            "\n\nTIP:\n%TS###<Text>%TE\n(eg. %TS083That Guy%TE)" +
            "\n\nWait for voice line to end\n%V (at end of dialogue)" +
            "\n\nX Position:\n%X### (before start of text)\n(eg. %X050 shift to 50 pixels from left)";
            _command = "Msg Disp2";

           
            CompleteMessage = _dataWrapper.ReadString(MsgPtr.MsgPtrString);
            MessagePointerList.Add(MsgPtr);

            int idx1 = CompleteMessage.IndexOf("「");
            int idx2 = CompleteMessage.IndexOf("」");

            if (idx1 > 0)
                Speaker = CompleteMessage.Substring(0, idx1);
            else
                Speaker = "";

            if (idx2 >= 0)
                MessageEnding = CompleteMessage.Substring(idx2 + 1, CompleteMessage.Length - idx2 - 1);
            else if (terminator_regex.IsMatch(CompleteMessage))
            {
                MatchCollection mc = terminator_regex.Matches(CompleteMessage);

                if (mc.Count != 1) throw new Exception("Expected only 1 result");

                MessageEnding = mc[0].Value;
                idx2 = CompleteMessage.Length - MessageEnding.Length;
            }
            else
                MessageEnding = "";

            if (idx2 >= 0)
            {
                if (idx1 > 0)
                    Message = CompleteMessage.Substring(idx1 + 1, idx2 - idx1 - 1);
                else if (idx1 == 0)
                    Message = CompleteMessage.Substring(0, idx2 + 1);
                else
                    Message = CompleteMessage.Substring(0, idx2);
            }
            else
                Message = CompleteMessage;

            // Remove double spaces
            Message = Utility.StringSingleSpace(Message);

            UpdateData();
        }

        public override string GetMessages()
        {
            return CompleteMessage;
        }


        public override byte[] GetMessagesBytes()
        {
            byte[] msg = Utility.StringEncode(CompleteMessage);
            byte[] output = new byte[msg.Length + 1];
            msg.CopyTo(output, 0);

            return output;
        }

        public override void UpdateData()
        {
            string message_spacing = Utility.StringDoubleSpace(Message);

            if (Speaker.Length > 0)
                CompleteMessage = Speaker + "「" + message_spacing + "」" + MessageEnding;
            else
                CompleteMessage = message_spacing + "" + MessageEnding;

            Data2 = CompleteMessage;
        }

        public override Token Clone()
        {
            return new TokenMsgDisp2(this);
        }

        public override void UpdateGui(MainWindow window)
        {
            base.UpdateGui(window);
            base.AddTextbox(window, "Speaker", "Speaker");
            base.AddRichTextbox(window, "Text (EN)", "Message");
            base.AddRichTextbox(window, "Text (JP)", "MessageJp");
            base.AddTextbox(window, "Terminator", "MessageEnding");
            base.AddRichTextbox(window, "Complete Text", "CompleteMessage", false);

            base.AddSpacer(window);
        }
    }
}
