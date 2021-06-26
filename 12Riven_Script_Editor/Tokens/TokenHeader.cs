using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Riven_Script_Editor.Tokens
{
    class TokenHeader : Token
    {

        public TokenHeader(DataWrapper wrapper, byte[] byteCommand, int pos) : base(wrapper, byteCommand, pos)
        {
            _command = "Header";
            

            _length = byteCommand[0];
        }

        public override string GetMessages()
        {
            return "";
        }
    }
}
