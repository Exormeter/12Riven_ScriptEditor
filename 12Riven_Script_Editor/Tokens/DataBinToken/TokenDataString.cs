using Riven_Script_Editor.Tokens;
using System;
using System.Linq;

namespace Riven_Script_Editor
{
    class TokenDataString : Token
    {
        private readonly MessagePointer dataStringPointer;

        public string DataString
        {
            get { return dataStringPointer.Message; }
            set { dataStringPointer.Message = value; }
        }
            
    public TokenDataString(DataWrapper dataWrapper, int offset) : base(dataWrapper, offset)
        {
            _length = 4;
            _command = "String";
            _byteCommand = dataWrapper.RawArray.Skip(offset).Take(_length).ToArray();

            dataStringPointer = new MessagePointer(1, 0, _byteCommand);
            MessagePointerList.Add(dataStringPointer);

            DataString = _dataWrapper.ReadString(dataStringPointer.MsgPtrString);
            Data2 = DataString;
        }

        public override byte[] GetMessagesBytes()
        {
            byte[] msg = Utility.StringEncode(DataString);
            byte[] output = new byte[msg.Length + 1];
            msg.CopyTo(output, 0);

            return output;
        }

        public override void UpdateGui(MainWindow window)
        {
            base.UpdateGui(window);
            base.AddTextbox(window, "Data String", "DataString");
        }

        public override string GetMessages()
        {
            return DataString;
        }
    }
}