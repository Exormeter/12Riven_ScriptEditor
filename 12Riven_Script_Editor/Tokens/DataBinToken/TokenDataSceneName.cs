using Riven_Script_Editor.Tokens;
using System;
using System.Linq;

namespace Riven_Script_Editor
{
    class TokenDataSceneName : Token
    {
        private readonly MessagePointer sceneNamePointer;

        public string SceneName { 
            get { return sceneNamePointer.Message; }
            set { sceneNamePointer.Message = value; }
        }
        public TokenDataSceneName(DataWrapper dataWrapper, int offset) : base(dataWrapper, offset)
        {
            _command = "Scene Name";
            _length = 8;

            _byteCommand = dataWrapper.RawArray.Skip(offset).Take(_length).ToArray();

            sceneNamePointer = new MessagePointer(1, 0, _byteCommand);
            MessagePointerList.Add(sceneNamePointer);

            SceneName = _dataWrapper.ReadString(sceneNamePointer.MsgPtrString);
            Data2 = SceneName;
        }

        public override byte[] GetMessagesBytes()
        {
            byte[] msg = Utility.StringEncode(SceneName);
            byte[] output = new byte[msg.Length + 1];
            msg.CopyTo(output, 0);

            return output;
        }

        public override void UpdateGui(MainWindow window)
        {
            base.UpdateGui(window);
            base.AddTextbox(window, "Scene Name", "SceneName");
        }
        public override string GetMessages()
        {
            return "";
        }
    }
}