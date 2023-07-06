using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riven_Script_Editor.Tokens
{
    internal class TokenMovieStart : Token
    {
        public new const TokenType Type = TokenType.movie_start;

        public MessagePointer MsgPtr;

        public string MovieFileName
        {
            get { return MsgPtr.Message; }
            set { MsgPtr.Message = value; }
        }

        public TokenMovieStart(Token token) : base(token)
        {
        }

        public TokenMovieStart(DataWrapper dataWrapper, int offset) : base(dataWrapper, offset)
        {
            MsgPtr = new MessagePointer(3, 2, _byteCommand);
            MessagePointerList.Add(MsgPtr);
            MovieFileName = _dataWrapper.ReadString(MsgPtr.MsgPtrString);
            Data2 = MovieFileName;
        }

        public TokenMovieStart(DataWrapper dataWrapper, byte[] byteCommand, int offset) : base(dataWrapper, byteCommand, offset)
        {
            MsgPtr = new MessagePointer(3, 2, _byteCommand);
            MessagePointerList.Add(MsgPtr);
            MovieFileName = _dataWrapper.ReadString(MsgPtr.MsgPtrString);
            Data2 = MovieFileName;
        }

        public override byte[] GetMessagesBytes()
        {
            byte[] msg = Utility.StringEncode(MovieFileName);
            byte[] output = new byte[msg.Length + 1];
            msg.CopyTo(output, 0);

            return output;
        }
    }
}
