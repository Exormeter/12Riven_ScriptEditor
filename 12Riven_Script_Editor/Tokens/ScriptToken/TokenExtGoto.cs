using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riven_Script_Editor.Tokens
{
    internal class TokenExtGoto : Token
    {
        
        public TokenExtGoto(DataWrapper dataWrapper, int offset) : base(dataWrapper, offset)
        {
            while (dataWrapper[offset + Length] == 0x00)
            {
                this._length++;
            }
            _byteCommand = dataWrapper.RawArray.Skip(offset).Take(_length).ToArray();
            Data = Utility.ToString(_byteCommand);

        }
    }
}
