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
        private Token trailerToken;
        bool _changed_file;
        int pos;
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
        }

        public override List<Token> ParseData()
        {
            var tokens = new List<Token>();
            pos = 0;

            int lenght = Convert.ToInt32(data[pos]);

            byte[] byteCommand = data.RawArray.Skip(pos).Take(lenght).ToArray();

            TokenHeader headerToken = new TokenHeader(data, byteCommand, pos);

            tokens.Add(headerToken);
            pos += headerToken.Length;

            while(true)
            {
                Token token = ReadNextToken(tokens);
                pos += token.Length;

                
                tokens.Add(token);
                
                if(token.OpCode == 0x0D)
                {
                    break;
                }
            }

            foreach(Token t in tokens.Where(token => token is TokenJump))
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
            int trailerLenght = data.RawArray.Length - trailerStart;
            byteCommand = data.RawArray.Skip(trailerStart).Take(trailerLenght).ToArray();
            trailerToken = new Token(data, byteCommand, 0);
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

            stream.Seek(0, SeekOrigin.End);
            byte[] trailerBuffer = trailerToken.ByteCommand;
            stream.Write(trailerBuffer, 0, trailerBuffer.Length);

            // Copy the stream to a byte array
            stream.Seek(0, SeekOrigin.Begin);
            byte[] output = new byte[stream.Length];
            stream.Read(output, 0, (int)stream.Length);
            return output;
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
            TokenType opcode = (TokenType)data[pos];
            int lenght = Convert.ToInt32(data[pos + 1]);

            byte[] byteCommand = data.RawArray.Skip(pos).Take(lenght).ToArray();

            switch (opcode) 
            {
                case TokenExtGoto.Type: return new TokenExtGoto(data, byteCommand, pos, _listFileManager.CompleteFilenameList[byteCommand[2]]);
                case TokenMsgDisp2.Type: return new TokenMsgDisp2(data, byteCommand, pos);
                case TokenSelectDisp2.Type: return new TokenSelectDisp2(data, byteCommand, pos);
                case TokenSysMessage.Type: return new TokenSysMessage(data, byteCommand, pos);
                case TokenJump.Type: return new TokenJump(data, byteCommand, pos, null);
                default: return new Token(data, byteCommand, pos);
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
