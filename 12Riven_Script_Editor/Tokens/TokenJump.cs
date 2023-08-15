using System;

namespace Riven_Script_Editor.Tokens
{
    class TokenJump : Token
    {
        public Token ReferencedToken { get; set; }

        public TokenJump(Token token, Token referencedToken) : base(token)
        {
            ReferencedToken = referencedToken;
        }

        public TokenJump(DataWrapper dataWrapper, int offset, Token referencedToken) : base(dataWrapper, offset)
        {
            ReferencedToken = referencedToken;
        }

        public TokenJump(DataWrapper dataWrapper, byte[] byteCommand, int offset, Token referencedToken) : base(dataWrapper, byteCommand, offset)
        {
            ReferencedToken = referencedToken;
        }

        public UInt16 GetReferencedOffset()
        {
            return (UInt16)(_byteCommand[2] | _byteCommand[3] << 8);
        }

        public override byte[] ByteCommand
        {
            get
            {
                byte[] referenceOffsetBytes = BitConverter.GetBytes(ReferencedToken.Offset);
                _byteCommand[2] = referenceOffsetBytes[0];
                _byteCommand[3] = referenceOffsetBytes[1];
                return _byteCommand;
            }
            set
            {
                _byteCommand = value;
            }
        }
    }
}
