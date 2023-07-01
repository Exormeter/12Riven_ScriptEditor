using System;
using System.Linq;

namespace Riven_Script_Editor.Tokens
{
    class TokenDataName : Token
    {
        private readonly MessagePointer namePointer;

        private readonly MessagePointer nameBracePointer;

        public string Name {
            get { return namePointer.Message; }
            set { namePointer.Message = value; } 
        }
        public string NameBraces {
            get { return nameBracePointer.Message; }
            set { nameBracePointer.Message = value; }
        }
       
        public TokenDataName(DataWrapper dataWrapper, int offset, bool blank = false) : base(dataWrapper, offset)
        {
            _length = 16;
            _command = "Name";

            _byteCommand = dataWrapper.RawArray.Skip(offset).Take(_length).ToArray();

            namePointer = new MessagePointer(5, 4, _byteCommand);
            nameBracePointer = new MessagePointer(1, 0, _byteCommand);
            MessagePointerList.Add(namePointer);
            MessagePointerList.Add(nameBracePointer);

            Name = _dataWrapper.ReadString(namePointer.MsgPtrString);
            NameBraces = _dataWrapper.ReadString(nameBracePointer.MsgPtrString);

            Data2 = Name;
        }

        public override byte[] GetMessagesBytes()
        {
            byte[] msg = Utility.StringEncode(NameBraces);
            byte[] output = new byte[msg.Length + 1];
            msg.CopyTo(output, 0);

            return output;
        }

        public override string GetMessages()
        {
            return Name;
        }

        public override void UpdateGui(MainWindow window)
        {
            base.UpdateGui(window);
            base.AddTextbox(window, "Name", "Name");
            base.AddTextbox(window, "Name Braces", "NameBraces");
        }
    }
}