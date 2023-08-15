using IntervalTree;
using Riven_Script_Editor.Tokens;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Riven_Script_Editor
{

    enum Section
    {
        HEADER,
        NAMES,
        ROUTE,
        ROUTE2,
        DATA,
        SCENE_NAME,
        DATA2,
        STRINGS,
        CHUNK,
        TITLE_NAME,
        FOOTER
    }

    class DataTokenizer : ATokenizer
    {
        private readonly int ptrSectionSize = 0x2650;
        private readonly int trailerStart = 0x5733;
        private readonly IntervalTree<int, Section> sectionTree = new IntervalTree<int, Section>();
        private Token trailerToken;

        public DataTokenizer(DataWrapper wrapper) : base(wrapper)
        { 
            sectionTree.Add( 0, 0x5F, Section.HEADER);
            sectionTree.Add(0x60, 0x88F, Section.NAMES);
            sectionTree.Add(0x890, 0xEBF, Section.ROUTE);
            sectionTree.Add(0xEC0, 0xF1F, Section.ROUTE2);
            sectionTree.Add(0xF20, 0x14BB, Section.DATA);
            sectionTree.Add(0x14BC, 0x1793, Section.SCENE_NAME);
            sectionTree.Add(0x1794, 0x1E37, Section.DATA2);

            //sectionTree.Add(0x1E38, 0x2443, Section.STRINGS);
            sectionTree.Add(0x1E38, 0x2453, Section.STRINGS);

            //sectionTree.Add(0x2444, 0x2447, Section.CHUNK);
            sectionTree.Add(0x2454, 0x2457, Section.CHUNK);

            //sectionTree.Add(0x2448, 0x2537, Section.TITLE_NAME);
            sectionTree.Add(0x2458, 0x2547, Section.TITLE_NAME);

            //sectionTree.Add(0x2538, 0x253B, Section.CHUNK);
            sectionTree.Add(0x2548, 0x254B, Section.CHUNK);

            //sectionTree.Add(0x253C, 0x261F, Section.STRINGS);
            sectionTree.Add(0x254C, 0x262F, Section.STRINGS);

            //sectionTree.Add(0x2620, 0x2623, Section.CHUNK);
            sectionTree.Add(0x2630, 0x2633, Section.CHUNK);

            //sectionTree.Add(0x2624, 0x262B, Section.STRINGS);
            sectionTree.Add(0x2634, 0x263B, Section.STRINGS);

            //sectionTree.Add(0x262C, 0x263F, Section.FOOTER);
            sectionTree.Add(0x263C, 0x264F, Section.FOOTER);
        }

        public override List<Token> ParseData()
        {
            var tokens = new List<Token>();
            
            int pos = 0;
            byte[] byteCommand;
            Section currentSection = Section.HEADER;
            while (pos < ptrSectionSize)
            {

                switch (currentSection)
                {
                    case Section.HEADER:
                        byteCommand = data.RawArray.Skip(pos).Take(Convert.ToInt32(data[pos])).ToArray();
                        tokens.Add(new TokenHeader(data, byteCommand, pos));
                        break;

                    case Section.NAMES:
                        tokens.Add(new TokenDataName(data, pos));
                        break;

                    case Section.ROUTE:
                        tokens.Add(new TokenDataRoute(data, pos));
                        break;

                    case Section.ROUTE2:
                        tokens.Add(new TokenDataRoute2(data, pos));
                        break;

                    case Section.DATA:
                        byteCommand = data.RawArray.Skip(pos).Take(1436).ToArray();
                        tokens.Add(new TokenDataChunk(data, byteCommand, pos, 1436));
                        break;

                    case Section.SCENE_NAME:
                        tokens.Add(new TokenDataSceneName(data, pos));
                        break;

                    case Section.DATA2:
                        byteCommand = data.RawArray.Skip(pos).Take(1700).ToArray();
                        tokens.Add(new TokenDataChunk(data, byteCommand, pos, 1700));
                        break;

                    case Section.STRINGS:
                        tokens.Add(new TokenDataString(data, pos));
                        break;

                    case Section.TITLE_NAME:
                        tokens.Add(new TokenDataTitleName(data, pos));
                        break;

                    case Section.CHUNK:
                        byteCommand = data.RawArray.Skip(pos).Take(4).ToArray();
                        tokens.Add(new TokenDataChunk(data, byteCommand, pos, 4));
                        break;

                    case Section.FOOTER:
                        byteCommand = data.RawArray.Skip(pos).Take(24).ToArray();
                        tokens.Add(new TokenDataChunk(data, byteCommand, pos, 24));
                        break;
                }

                pos += tokens.Last().Length;
                currentSection = sectionTree.Query(pos).FirstOrDefault();
            }

            int trailerLenght = data.RawArray.Length - trailerStart;
            byteCommand = data.RawArray.Skip(trailerStart).Take(trailerLenght).ToArray();
            trailerToken = new Token(data, byteCommand, 0);
            return tokens;
        }

        public override byte[] AssembleAsData(List<Token> tokens)
        {
            int offsetTextSection = 0x2640;
            int pos = 0;
            Stream stream = new MemoryStream();

            //padding with 0 until textSection begins
            byte[] fill = new Byte[offsetTextSection];
            stream.Write(fill, 0, fill.Length);
            
            foreach(var token in tokens)
            {
                token.MessagePointerList.Sort();
                foreach(MessagePointer messagePointer in token.MessagePointerList)
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

            streamWrite.WriteLine("");
            streamWrite.WriteLine("");

            foreach (var token in tokens)
            {
                if(token.GetMessages() != "")
                {
                    string message = token.Command + ": " + token.GetMessages();
                    streamWrite.WriteLine(message);
                    streamWrite.WriteLine("");
                }

            }
            streamWrite.Flush();
            memoryStream.Seek(0, SeekOrigin.Begin);
            byte[] output = new byte[memoryStream.Length];
            memoryStream.Read(output, 0, (int)memoryStream.Length);
            return output;
        }
    }
}