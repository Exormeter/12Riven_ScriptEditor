using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Riven_Script_Editor.Tokens
{
    internal class TokenExtGoto : Token
    {
        public string referencedFilename { get; set; }
        public TokenExtGoto(DataWrapper dataWrapper, byte[] byteCommand, int offset, string referencedFilename) : base(dataWrapper, byteCommand, offset)
        {
            this.referencedFilename = referencedFilename;
        }
    }
}
