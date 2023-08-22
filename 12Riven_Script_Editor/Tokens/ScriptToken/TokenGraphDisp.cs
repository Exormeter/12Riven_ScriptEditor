using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riven_Script_Editor.Tokens.ScriptToken
{
    public class TokenGraphDisp : Token
    {
        public TokenGraphDisp(Token token) : base(token)
        {
        }

        public TokenGraphDisp(DataWrapper dataWrapper, int offset) : base(dataWrapper, offset)
        {
            _length += _byteCommand[1] * 0x10;
            _byteCommand = dataWrapper.RawArray.Skip(offset).Take(_length).ToArray();
            Data = Utility.ToString(_byteCommand);
        }
    }
}
