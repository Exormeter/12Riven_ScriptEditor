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
    class Opcode
    {
        
        public string name;
        public byte opcodeByte;
        public int length;

        public Opcode(string name, byte opcodeByte, int length)
        {
            this.name = name;
            this.opcodeByte = opcodeByte;
            this.length = length;
        }
    }

    class ScriptTokenizer : ATokenizer
    {
        private Token trailerToken;
        private bool _changed_file;
        private int pos;

        static public List<Opcode> OpcodeList = new List<Opcode>
        {
            new Opcode("nop", 0x00, 2),
            new Opcode("end", 0x01, 2),
            new Opcode("if", 0x02, 4),
            new Opcode("int_goto", 0x03, 0xFF),
            new Opcode("int_call", 0x04, 0xFF),
            new Opcode("int_return", 0x05, 0xFF),
            new Opcode("ext_goto", 0x06, 12),
            new Opcode("ext_call", 0x07, 12),
            new Opcode("ext_return", 0x08, 2),
            new Opcode("reg_calc", 0x09, 6),
            new Opcode("count_clear", 0x0A, 2),
            new Opcode("count_wait", 0x0B, 4),
            new Opcode("time_wait", 0x0C, 4),
            new Opcode("pad_wait", 0x0D, 4),
            new Opcode("pad_get", 0x0E, 4),
            new Opcode("file_read", 0x0F, 4),
            new Opcode("file_wait", 0x10, 2),
            new Opcode("msg_wind", 0x11, 2),
            new Opcode("msg_view", 0x12, 2),
            new Opcode("msg_mode", 0x13, 2),
            new Opcode("msg_pos,", 0x14, 6),
            new Opcode("msg_size", 0x15, 6),
            new Opcode("msg_type", 0x16, 2),
            new Opcode("msg_coursor", 0x17, 6),
            new Opcode("msg_set", 0x18, 4),
            new Opcode("msg_wait", 0x19, 2),
            new Opcode("msg_clear", 0x1A, 2),
            new Opcode("msg_line", 0x1B, 2),
            new Opcode("msg_speed", 0x1C, 2),
            new Opcode("msg_color", 0x1D, 2),
            new Opcode("msg_anim", 0x1E, 2),
            new Opcode("msg_disp", 0x1F, 10),
            new Opcode("sel_set", 0x20, 4),
            new Opcode("sel_entry", 0x21, 6),
            new Opcode("sel_view", 0x22, 2),
            new Opcode("sel_wait", 0x23, 0xFF),
            new Opcode("sel_style", 0x24, 2),
            new Opcode("sek_disp", 0x25, 4),
            new Opcode("fade_start", 0x26, 4),
            new Opcode("fade_wait", 0x27, 2),
            new Opcode("graph_set", 0x28, 4),
            new Opcode("graph_del", 0x29, 2),
            new Opcode("graph_cpy", 0x2A, 4),
            new Opcode("grsaph_view", 0x2B, 6),
            new Opcode("graph_pos", 0x2C, 6),
            new Opcode("graph_move", 0x2D, 0xFF),
            new Opcode("graph_prio", 0x2E, 4),
            new Opcode("graph_anim", 0x2F, 4),
            new Opcode("graph_pal", 0x30, 4),
            new Opcode("graph_lay", 0x31, 4),
            new Opcode("graph_wait", 0x32, 4),
            new Opcode("graph_disp", 0x33, 0xFF),
            new Opcode("effect_start", 0x34, 4),
            new Opcode("effect_end", 0x35, 2),
            new Opcode("effect_wait", 0x36, 2),
            new Opcode("bgm_set", 0x37, 2),
            new Opcode("bgm_del", 0x38, 2),
            new Opcode("bgm_req", 0x39, 2),
            new Opcode("bgm_wait", 0x3A, 2),
            new Opcode("bgm_speed", 0x3B, 4),
            new Opcode("bgm_vol", 0x3C, 2),
            new Opcode("se_set", 0x3D, 2),
            new Opcode("se_del", 0x3E, 2),
            new Opcode("se_req", 0x3F, 4),
            new Opcode("se_wait", 0x40, 4),
            new Opcode("se_speed", 0x41, 4),
            new Opcode("se_vol", 0x42, 4),
            new Opcode("voice_set", 0x43, 2),
            new Opcode("voice_del", 0x44, 2),
            new Opcode("voice_req", 0x45, 2),
            new Opcode("voice_wait", 0x46, 2),
            new Opcode("voice_speed", 0x47, 4),
            new Opcode("voice_vol", 0x48, 2),
            new Opcode("menu_lock", 0x49, 2),
            new Opcode("save_lock", 0x4A, 2),
            new Opcode("save_check", 0x4B, 4),
            new Opcode("save_disp", 0x4C, 4),
            new Opcode("disk_change", 0x4D, 4),
            new Opcode("jamp_start", 0x4E, 4),
            new Opcode("jamp_end", 0x4F, 2),
            new Opcode("task_entry", 0x50, 4),
            new Opcode("task_del", 0x51, 2),
            new Opcode("cal_disp", 0x52, 4),
            new Opcode("title_disp", 0x53, 2),
            new Opcode("vib_start", 0x54, 4),
            new Opcode("vib_end", 0x55, 2),
            new Opcode("vib_wait", 0x56, 2),
            new Opcode("map_view", 0x57, 4),
            new Opcode("map_entry", 0x58, 4),
            new Opcode("map_disp", 0x59, 4),
            new Opcode("edit_view", 0x5A, 4),
            new Opcode("chat_send", 0x5B, 4),
            new Opcode("chat_msg", 0x5C, 4),
            new Opcode("chat_entry", 0x5D, 4),
            new Opcode("chat_exit", 0x5E, 4),
            new Opcode("null", 0x5F, 1),
            new Opcode("movie_play", 0x60, 4),
            new Opcode("graph_pos_auto", 0x61, 12),
            new Opcode("graph_pos_save", 0x62, 2),
            new Opcode("graph_uv_auto", 0x63, 16),
            new Opcode("graph_uv_save", 0x64, 2),
            new Opcode("effect_ex", 0x65, 38),
            new Opcode("fade_ex", 0x66, 0xFF),
            new Opcode("vib_ex", 0x67, 6),
            new Opcode("clock_disp", 0x68, 6),
            new Opcode("graph_disp_ex", 0x69, 0xFF),
            new Opcode("map_init_ex", 0x6A, 4),
            new Opcode("map_point_ex", 0x6B, 4),
            new Opcode("map_route_ex", 0x6C, 4),
            new Opcode("quick_save", 0x6D, 2),
            new Opcode("trace_pc", 0x6E, 2),
            new Opcode("sys_msg", 0x6F, 4),
            new Opcode("skip_lock", 0x70, 2),
            new Opcode("key_lock", 0x71, 2),
            new Opcode("graph_disp2", 0x72, 0xFF),
            new Opcode("msg_disp2", 0x73, 12),
            new Opcode("sel_disp2", 0x74, 6),
            new Opcode("date_disp", 0x75, 8),
            new Opcode("vr_disp", 0x76, 4),
            new Opcode("vr_select", 0x77, 4),
            new Opcode("vr_reg_calc", 0x78, 4),
            new Opcode("vr_msg_disp", 0x79, 4),
            new Opcode("map_select", 0x7A, 4),
            new Opcode("ecg_set", 0x7B, 4),
            new Opcode("ev_init", 0x7C, 4),
            new Opcode("ev_disp", 0x7D, 4),
            new Opcode("ev_anim", 0x7E, 4),
            new Opcode("eye_lock", 0x7F, 2),
            new Opcode("msg_log", 0x80, 4),
            new Opcode("graph_scale_auto", 0x81, 16),
            new Opcode("movie_start", 0x82, 2),
            new Opcode("move_end", 0x83, 2),
            new Opcode("fade_ex_strt", 0x84, 6),
            new Opcode("fade_ex_wait", 0x85, 2),
            new Opcode("breath_lock", 0x86, 2),
            new Opcode("g3d_disp", 0x87, 2),
            new Opcode("staff_start", 0x88, 6),
            new Opcode("staff_end", 0x89, 2),
            new Opcode("staff_wait", 0x8A, 2),
            new Opcode("scroll_lock", 0x8B, 2)
        };

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

            //byte[] byteCommand = data.RawArray.Skip(pos).Take(lenght).ToArray();

            //TokenHeader headerToken = new TokenHeader(data, byteCommand, pos);

            //tokens.Add(headerToken);
            //pos += headerToken.Length;

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
            //byteCommand = data.RawArray.Skip(trailerStart).Take(trailerLenght).ToArray();
            //trailerToken = new Token(data, byteCommand, 0);
            //tokens.Add(trailerToken);
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
            Console.Write("Parsing at offset: " + pos);
            if (data[pos] > 0x8B) { throw new NotSupportedException("Opcode not supported"); }

            Opcode opcode = OpcodeList[data[pos]];

            if (opcode.length == 0xFF) { throw new NotSupportedException("Opcode Length unknown"); }
        
            Console.Write("Opcode: " + opcode.ToString());
            Console.Write(' ');

            byte[] byteCommand = data.RawArray.Skip(pos).Take(opcode.length).ToArray();

            switch (opcode.opcodeByte) 
            {
                case 0x73: return new TokenMsgDisp2(data, byteCommand, pos);
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
