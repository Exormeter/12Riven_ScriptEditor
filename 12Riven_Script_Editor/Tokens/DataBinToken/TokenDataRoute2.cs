using Riven_Script_Editor.Tokens;
using System.Linq;

namespace Riven_Script_Editor
{
    internal class TokenDataRoute2 : Token
    {
        private readonly MessagePointer routeOnePointer;

        private readonly MessagePointer routeTwoPointer;

        public string Route1
        {
            get { return routeOnePointer.Message; }
            set { routeOnePointer.Message = value; }
        }
        public string Route2
        {
            get { return routeTwoPointer.Message; }
            set { routeTwoPointer.Message = value; }
        }
        public TokenDataRoute2(DataWrapper dataWrapper, int offset) : base(dataWrapper, offset)
        {
            _length = 24;
            _command = "RouteName";

            _byteCommand = dataWrapper.RawArray.Skip(offset).Take(_length).ToArray();
            
            MessagePointerList.Add(new MessagePointer(0, 0, Utility.StringDecode(new byte[] { 0x0})));
            routeOnePointer = new MessagePointer(9, 8, _byteCommand);
            routeTwoPointer = new MessagePointer(13, 12, _byteCommand);


            MessagePointerList.Add(routeTwoPointer);
            MessagePointerList.Add(routeOnePointer);

            Route1 = _dataWrapper.ReadString(routeOnePointer.MsgPtrString);
            Route2 = _dataWrapper.ReadString(routeTwoPointer.MsgPtrString);
            Data2 = Route1 + " // " + Route2;
        }

        public override byte[] GetMessagesBytes()
        {
            byte[] msg = Utility.StringEncode(Route1);
            byte[] output = new byte[msg.Length + 1];
            msg.CopyTo(output, 0);

            return output;
        }

        public override void UpdateGui(MainWindow window)
        {
            base.UpdateGui(window);
            base.AddTextbox(window, "Scene Name", "Route1");
            base.AddTextbox(window, "Time", "Route2");
        }

        public override string GetMessages()
        {
            return Route1 + " // " + Route2;
        }

    }
}