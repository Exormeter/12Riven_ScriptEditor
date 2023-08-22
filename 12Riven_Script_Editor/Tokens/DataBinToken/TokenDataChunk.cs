using Riven_Script_Editor.Tokens;

namespace Riven_Script_Editor
{
    internal class TokenDataChunk : Token
    {
        public TokenDataChunk(DataWrapper dataWrapper, int offset, int length) : base(dataWrapper, offset)
        {
            _command = "Chunk";
            _length = length;
        }

        public override string GetMessages()
        {
            return "";
        }
    }
    
}