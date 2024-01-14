/*
 * Issues:
 * Automatically create archive
 *  Randomly spits out gibberish then dies
 *  Text on choices is misaligned severely
 *  I think the way I made TokenIf create an IntGoto token is messing things up
 *      Nope, something else is causing it. Look for commands that happen before it screws up.
 */


using Riven_Script_Editor.FileTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;


namespace Riven_Script_Editor.Tokens
{
    class ScriptTokenizer : ATokenizer
    {
        private bool _changed_file;
        private int pos;
        private LzssCompress compressor = new LzssCompress();

        public bool ChangedFile
        {
            get { return _changed_file; }
            set {
                _changed_file = value;
                var x = ((MainWindow)Application.Current.MainWindow);
                if (!value && x.Title.EndsWith("*"))
                    x.Title = x.Title.Substring(0, x.Title.Length - 2);
                else if (value && !x.Title.EndsWith("*"))
                    x.Title += " *";
            }
        }
        private ScriptListFileManager _listFileManager;


        public ScriptTokenizer(DataWrapper wrapper, ScriptListFileManager scriptListFileManager) : base(wrapper)
        {
            _listFileManager = scriptListFileManager;

            byte[] decompressedData = compressor.Decompress(data.Slice(4, data.RawArray.Length - 4)); //LZSS0 compression starts at offset 4
            data = new DataWrapper(decompressedData);
        }

        public override List<Token> ParseData()
        {

            var tokens = new List<Token>();
            pos = 0;

            int lenght = Convert.ToInt32(data[pos]);

            while (true)
            {
                Token token = ReadNextToken(tokens);
                pos += token.Length;


                tokens.Add(token);

                if (token.OpCode == 0x06 || token.OpCode == 0x01)
                {
                    break;
                }
            }

            foreach (Token t in tokens.Where(token => token is TokenJump))
            {
                TokenJump jumpToken = (TokenJump)t;

                jumpToken.ReferencedToken = tokens.Find(token => token.Offset == jumpToken.GetReferencedOffset());
                int jumpTokenIndex = tokens.IndexOf(jumpToken);
                int referncedTokenIndex = tokens.IndexOf(jumpToken.ReferencedToken);
                int distance = Math.Abs(jumpTokenIndex - referncedTokenIndex);
                int index = Math.Min(jumpTokenIndex, referncedTokenIndex);
                int stopIndex = index + distance + 1;
                for (; index < stopIndex; index++)
                {
                    tokens[index].Splitable = "No";
                }
            }

            TokenMsgDisp2 lastMsgToken = (TokenMsgDisp2)tokens.Last(tempToken => tempToken is TokenMsgDisp2);

            int trailerStart = lastMsgToken.MsgPtr.MsgPtrString + lastMsgToken.GetMessagesBytes().Length;
            var trailerToken = new ScriptToken.TokenTrailer(data, trailerStart);
            tokens.Add(trailerToken);
            return tokens;
        }

        public override byte[] AssembleAsData(List<Token> tokens)
        {
            int offsetTextSection = 0;
            int pos = 0;
            Stream stream = new MemoryStream();
            updateOffsets(tokens);
            tokens.ForEach(token => offsetTextSection += token.Length);

            //padding with 0 until textSection begins
            byte[] fill = new Byte[offsetTextSection];
            stream.Write(fill, 0, fill.Length);

            // data at the end of the script behind the text section
            ScriptToken.TokenTrailer lastToken = null;
            if (tokens.Last() is ScriptToken.TokenTrailer)
            {
                lastToken = (ScriptToken.TokenTrailer)tokens.Last();
                tokens.RemoveAt(tokens.Count - 1);
            }

            foreach (var token in tokens)
            {
                token.MessagePointerList.Sort();
                foreach (MessagePointer messagePointer in token.MessagePointerList)
                {
                    messagePointer.MsgPtrString = (UInt16)stream.Length;
                    stream.Seek(0, SeekOrigin.End);
                    byte[] message = messagePointer.GetMessagesBytes();
                    stream.Write(message, 0, message.Length);
                }

                stream.Seek(pos, SeekOrigin.Begin);
                byte[] buffer = token.ByteCommand;
                stream.Write(buffer, 0, buffer.Length);

                pos += token.Length;
            }

            if (lastToken != null)
            {
                stream.Seek(0, SeekOrigin.End);
                stream.Write(lastToken.ByteCommand, 0, lastToken.ByteCommand.Length);
            }
            // Copy the stream to a byte array
            stream.Seek(0, SeekOrigin.Begin);
            byte[] output = new byte[stream.Length];
            stream.Read(output, 0, (int)stream.Length);

            //return output;
            return compressor.Compress(output);
        }

        public override byte[] AssembleAsText(string title, List<Token> tokens)
        {
            Stream memoryStream = new MemoryStream();
            StreamWriter streamWrite = new StreamWriter(memoryStream, Encoding.GetEncoding(932));

            streamWrite.WriteLine("Scene: " + title);
            streamWrite.WriteLine("");
            streamWrite.WriteLine("");
            int index = 1;
            foreach (var token in tokens)
            {
                string message = "";
                if (token is TokenMsgDisp2 tokenMsgDisp2)
                {
                    message = tokenMsgDisp2.CompleteMessage;
                    
                }

                else if (token is TokenSelectDisp2 tokenSelectDisp2)
                {
                    foreach (SelectDisp2Entry entry in tokenSelectDisp2.Entries)
                    {
                        message += entry.Message + " / ";
                    }
                }

                if(message != "")
                {
                    message = index.ToString() + ". " + message;
                    streamWrite.WriteLine(message);
                    streamWrite.WriteLine("");
                    index++;
                }
                
            }
            streamWrite.Flush();
            memoryStream.Seek(0, SeekOrigin.Begin);
            byte[] output = new byte[memoryStream.Length];
            memoryStream.Read(output, 0, (int)memoryStream.Length);
            return output;
        }

        private Token ReadNextToken(List<Token> pastParsedTokens)
        {
            Console.Write("Parsing at offset: " + pos);
            if (data[pos] > 0x8B) { throw new NotSupportedException("Opcode not supported"); }

            Opcode opcode = Token.OpcodeList[data[pos]];

            if (opcode.length == 0xFF) { throw new NotSupportedException("Opcode Length unknown"); }
        
            Console.Write("Opcode: " + opcode.ToString());
            Console.Write(' ');


            switch (opcode.opcodeByte) 
            {
                case 0x06: return new TokenExtGoto(data, pos);
                case 0x33: return new ScriptToken.TokenGraphDisp(data, pos);
                case 0x73: return new TokenMsgDisp2(data, pos);
                case 0x74: return new TokenSelectDisp0x74(data, pos);
                default: return new Token(data, pos);
            }
        }

        private void updateOffsets(List<Token> tokens)
        {
            int offset = 0;
            foreach (Token t in tokens)
            {
                t.Offset = (UInt16)offset;
                offset += t.Length;
            }
        }
    }
}
