using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riven_Script_Editor.Tokens.ScriptToken
{
    class TokenTrailer : Token
    {
        public TokenTrailer(Token token) : base(token)
        {
            throw new NotImplementedException();
        }

        public TokenTrailer(DataWrapper dataWrapper, int offset) : base(dataWrapper, offset)
        {
            _length = dataWrapper.RawArray.Length - offset;
            _byteCommand = dataWrapper.Slice(offset, _length);
        }
    }
}
