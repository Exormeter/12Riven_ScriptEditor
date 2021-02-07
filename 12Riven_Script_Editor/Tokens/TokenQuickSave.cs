using System;
using System.Collections.Generic;

namespace Riven_Script_Editor.Tokens
{
    class TokenQuickSave : Token
    {
        public new const TokenType Type = TokenType.quick_save;
        
        byte fixed1;

        public TokenQuickSave(byte[] byteCommand, int pos, bool blank=false): base(byteCommand, pos)
        {
            _command = "Quick Save";
            _description = "Quick save, usually done at the start of a new scene.";

            fixed1 = 2;
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
