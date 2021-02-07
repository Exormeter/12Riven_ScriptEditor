using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Riven_Script_Editor.Tokens
{
    class TokenHeader : Token
    {
        public new const TokenType Type = TokenType.bgm_del;

        byte fixed1;

        public TokenHeader(byte[] byteCommand, int pos) : base(byteCommand, pos)
        {
            _command = "Header";
            

            _length = byteCommand[0];


            fixed1 = 0;
        }

        public override byte[] GetBytes()
        {
            byte[] output = new byte[_length];
            output[0] = (byte)Type;
            output[1] = (byte)fixed1;

            return output;
        }
    }
}
