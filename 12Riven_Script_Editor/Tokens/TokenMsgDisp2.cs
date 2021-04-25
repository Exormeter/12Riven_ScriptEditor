﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Riven_Script_Editor.Tokens
{
    class TokenMsgDisp2 : Token
    {
        public new const TokenType Type = TokenType.msg_disp2;

        private UInt16 _msgPtr;
        public UInt16 MsgPtr {
            get { return _msgPtr; }
            set
            {
                _byteCommand[7] = BitConverter.GetBytes(value)[1];
                _byteCommand[6] = BitConverter.GetBytes(value)[0];
                _msgPtr = value;
            }
        }
        public UInt16 MsgId { get; set; }
        public UInt16 VoiceId { get; set; }
        public MsgSpeaker SpeakerId { get; set; }
        public string CompleteMessage { get; set; }
        public string Speaker { get; set; }
        public string Message { get; set; }
        public string MessageEnding { get; set; }
        public string MessageJp { get; set; }
        private List<string> _identical_jp_labels = new List<string>();
        public List<string> IdenticalJpLabels {
            get =>  _identical_jp_labels;
            set => _identical_jp_labels = value; }

        static Regex terminator_regex = new Regex(@"(%[%ABCDEKNPpSTV0-9]*?)$");

        public TokenMsgDisp2(DataWrapper wrapper, byte[] byteCommand, int pos, bool blank=false): base(wrapper, byteCommand, pos)
        {

            MsgPtr = BitConverter.ToUInt16(byteCommand, 6);

            _command = "Msg Disp2";
            _description = "Displays text spoken by a character.\n" +
                "\nSpeaker ID: The sprite to animate" + 
                "\n\nColor:\n%C<RGBA>\n(eg. %C8CFF would give cyan, %CF66A is a slightly transparent red. Return to white using %CFFFF)" +
                "\n\nFade in:\n%FS<Text>%FE" +
                "\n\nWait for user input:\n%K" +
                "\n\nAlignment:\n%LR (right-align)\n%LC (center)" +
                "\n\nNew line:\n%N"+
                "\n\nFade out text:\n%O (used in the \"Infinity Loop\" Message)"+
                "\n\nClear text:\n%P (usually at end of dialogue)"+
                "\n\nTime delay?:\n%T##\n(eg. %T60 = delay of 60 ticks)" +
                "\n\nTIP:\n%TS###<Text>%TE\n(eg. %TS083That Guy%TE)" +
                "\n\nWait for voice line to end\n%V (at end of dialogue)" +
                "\n\nX Position:\n%X### (before start of text)\n(eg. %X050 shift to 50 pixels from left)";

            MessageJp = _dataWrapper.ReadString(MsgPtr);
            if (blank)
            {
                MsgPtr = 0;
                CompleteMessage = "Message Empty%K%P";

                MsgId = 0;
                VoiceId = 0;
                SpeakerId = MsgSpeaker.Narrator;
            }
            else
            {
                CompleteMessage = _dataWrapper.ReadString(MsgPtr);

                MsgId = _dataWrapper.ReadUInt16(4);
                VoiceId = _dataWrapper.ReadUInt16(6);
                //SpeakerId = (MsgSpeaker)Tokenizer.ReadUInt16(8); if (!Enum.IsDefined(typeof(MsgSpeaker), SpeakerId)) throw new ArgumentOutOfRangeException();
            }



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
            if (MessageJp != null)
                return CompleteMessage + MessageJp;
            return CompleteMessage;
        }

        public override byte[] GetMessagesBytes()
        {
            byte[] msg = Utility.StringEncode(CompleteMessage);
            byte[] output = new byte[msg.Length + 1];
            msg.CopyTo(output, 0);

            return output;
        }

        public override int SetMessagePointer(int offset)
        {
            MsgPtr = (UInt16) offset;
            return offset + Utility.StringEncode(CompleteMessage).Length + 1;
        }

        public override void UpdateData()
        {
            string message_spacing = Utility.StringDoubleSpace(Message);

            if (Speaker.Length > 0)
                //CompleteMessage = Speaker + "「" + Message + "」" + MessageEnding;
                CompleteMessage = Speaker + "「" + message_spacing + "」" + MessageEnding;
            else
                CompleteMessage = message_spacing + "" + MessageEnding;

            Data2 = CompleteMessage;
        }

        public override void UpdateGui(MainWindow window)
        {
            base.UpdateGui(window);
            base.AddTextbox(window, "Speaker", "Speaker");
            base.AddRichTextbox(window, "Message", "Message");
            base.AddTextbox(window, "Terminator", "MessageEnding");
            base.AddRichTextbox(window, "Complete Text", "CompleteMessage", false);
            base.AddTranslationButton(window, "Translation", "MessageJp");

            base.AddSpacer(window);
        }
    }

    public enum MsgSpeaker
    {
        Narrator = 0x00,
        Kokoro = 0x01,
        Satoru = 0x02,
        Mayuzumi = 0x03,
        Yomogi = 0x04,
        Utsumi = 0x05,
        Inubushi = 0x06,
        Yuni = 0x07,
        Enomoto = 0x09,
        Sayaka = 0x0a,
        Yomogi_Mayuzumi = 0xc4,
        Kokoro_Yuni_Mayuzumi = 0x70c1,
        Yuni_Yomogi_Mayuzumi = 0x3107,
    }
}
