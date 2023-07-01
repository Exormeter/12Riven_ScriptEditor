using Riven_Script_Editor.Tokens;
using System;
using System.Linq;

namespace Riven_Script_Editor
{
    internal class TokenDataTitleName : Token
    {

        private readonly MessagePointer titleNamePointer;
        public string Title 
        {
            get { return titleNamePointer.Message; }
            set { titleNamePointer.Message = value; }
        }

    public TokenDataTitleName(DataWrapper dataWrapper, int offset) : base(dataWrapper, offset)
        {
            _length = 8;
            _command = "Title Name";
            _byteCommand = dataWrapper.RawArray.Skip(offset).Take(_length).ToArray();

            titleNamePointer = new MessagePointer(1, 0, _byteCommand);
            Title = _dataWrapper.ReadString(titleNamePointer.MsgPtrString);
            MessagePointerList.Add(titleNamePointer);

            Data2 = Title;
        }

        public override byte[] GetMessagesBytes()
        {
            byte[] msg = Utility.StringEncode(Title);
            byte[] output = new byte[msg.Length + 1];
            msg.CopyTo(output, 0);

            return output;
        }

        public override void UpdateGui(MainWindow window)
        {
            base.UpdateGui(window);
            base.AddTextbox(window, "Title", "Title");
        }

        public override string GetMessages()
        {
            return Title;
        }
    }
}