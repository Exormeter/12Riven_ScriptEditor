using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Xceed.Wpf.Toolkit;
using System.Linq;

namespace Riven_Script_Editor.Tokens
{

    public class MessagePointer : IComparable<MessagePointer>
    {
        private readonly int _upperByte;
        private readonly int _lowerByte;
        private byte[] _commandData;
        public string Message { get; set; }

        private UInt16 _msgPtrString;
        public UInt16 MsgPtrString
        {
            get { return _msgPtrString; }
            set
            {
                if (_commandData == null) return;
                _commandData[_upperByte] = BitConverter.GetBytes(value)[1];
                _commandData[_lowerByte] = BitConverter.GetBytes(value)[0];
                _msgPtrString = value;
            }
        }

        public MessagePointer(int upper, int lower, byte[] commandData)
        {
            _upperByte = upper;
            _lowerByte = lower;
            _commandData = commandData;
            MsgPtrString = BitConverter.ToUInt16(commandData, lower);
            
        }

        public MessagePointer(int upper, int lower, String message)
        {
            _upperByte = upper;
            _lowerByte = lower;
            Message = message;
        }

        public int CompareTo(MessagePointer other)
        {
            return _lowerByte.CompareTo(other._lowerByte);
        }

        public byte[] GetMessagesBytes()
        {
            byte[] msg = Utility.StringEncode(Message);
            byte[] output = new byte[msg.Length + 1];
            msg.CopyTo(output, 0);

            return output;
        }
    }

    public class Opcode
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



    public class Token : INotifyPropertyChanged
    {
        private SolidColorBrush BrushForeground = new SolidColorBrush(Color.FromRgb(0,0,0));
        private SolidColorBrush BrushBackground = new SolidColorBrush(Color.FromRgb(1, 1, 1));

        public List<MessagePointer> MessagePointerList = new List<MessagePointer>();

        static public List<Opcode> OpcodeList = new List<Opcode>
        {
            new Opcode("nop", 0x00, 0xFF),
            new Opcode("end", 0x01, 2),
            new Opcode("if", 0x02, 10),
            new Opcode("int_goto", 0x03, 4), // probably 4
            new Opcode("int_call", 0x04, 4), // probably 4
            new Opcode("int_return", 0x05, 0xFF),
            new Opcode("ext_goto", 0x06, 4),
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
            new Opcode("graph_disp", 0x33, 4), // 4 is minimum, Token length is variable
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
            new Opcode("graph_disp_ex", 0x69, 0x18),
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

        public Token(DataWrapper dataWrapper, int offset)
        {
            _opCode = dataWrapper[offset];
            if (_opCode <= OpcodeList.Count)
            {
                _length = OpcodeList[_opCode].length;
                _byteCommand = dataWrapper.RawArray.Skip(offset).Take(_length).ToArray();
                _command = OpcodeList[_opCode].name;
                _offset = offset;
                _dataWrapper = dataWrapper;
                Data = Utility.ToString(_byteCommand);
            }
        }

        public Token(Token token)
        {
            byte[] tempCommand = new byte[token._byteCommand.Length];
            token.ByteCommand.CopyTo(tempCommand, 0);
            _byteCommand = tempCommand;
            _opCode = ByteCommand[0];
            _length = Convert.ToInt32(ByteCommand[1]);
            _command = _opCode.ToString("X2");
            _offset = 0;
            _dataWrapper = token._dataWrapper;
            Data = Utility.ToString(tempCommand);
        }
  
        protected DataWrapper _dataWrapper;

        protected byte[] _byteCommand;

        public virtual byte[] ByteCommand
        {
            get => _byteCommand;
            set
            {
                _byteCommand = value;
            }
        }
        
        protected byte _opCode;
        public byte OpCode
        {
            get => _opCode;
        }

        protected string _splitable = "yes";
        public string Splitable
        {
            get => _splitable;
            set
            {
                _splitable = value;
            }
        }

        protected string _command;
        public string Command
        {
            get => _command;
        }

        protected string _description;
        public string Description
        {
            get => _description;
        }

        protected int _offset;
        public UInt16 Offset
        {
            get => (UInt16)_offset;
            set => _offset = value;
        }

        public int Size
        {
            get => ByteCommand.Length + GetMessagesBytes().Length;
        }


        public string OffsetHex
        {
            get => _offset.ToString("X2");
        }

        protected int _length;
        public int Length
        {
            get { return _length; }
        }

        private List<string> _refering_labels = new List<string>();
        public List<string> ReferingLabels
        {
            get => _refering_labels;
            set
            {
                _refering_labels = value;
                OnPropertyChanged(nameof(ReferingLabels));
            }
        }

        private string _data;
        public string Data
        {
            get => _data;
            set
            {
                _data = value;
                if (Utility.ToByteArray(value, out _byteCommand))
                {
                    OnPropertyChanged(nameof(Data));
                }
            }
        }

        private string _data2;
        public string Data2
        {
            get => _data2;
            set
            {
                _data2 = value;
                OnPropertyChanged(nameof(Data2));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
           PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public virtual string GetMessages() { return ""; }

        public virtual byte[] GetMessagesBytes() { return new byte[0]; }

        public virtual int SetMessagePointer(int offset) { return offset; }

        public virtual void UpdateData() { }

        public virtual void UpdateGui(MainWindow window)
        {
            UpdateGui(window, true);
            AddTextbox(window, "Command", "Data");

            var row1 = new RowDefinition();
            row1.Height = new GridLength(24, GridUnitType.Pixel);
            window.Grid.Height += 24;
            window.Grid.RowDefinitions.Add(row1);
        }

        public virtual Token Clone()
        {
            return new Token(this);
        }

        public virtual void UpdateGui(MainWindow window, bool clear_list)
        {
            window.Grid.Children.Clear();
            window.Grid.RowDefinitions.Clear();
            window.Grid.Height = 0;
            if (clear_list)
                window.EntriesList.ItemsSource = null;

            var rows = window.Grid.RowDefinitions;

            AddDescriptionBox(window);
            if (!window.MenuViewDescription.IsChecked)
            {
                rows[rows.Count - 1].Height = new GridLength(0, GridUnitType.Pixel);
                window.Grid.Height -= 60;
            }


            AddSpacer(window);
            if (!window.MenuViewLabel.IsChecked && !window.MenuViewDescription.IsChecked)
            {
                rows[rows.Count - 1].Height = new GridLength(0, GridUnitType.Pixel);
                window.Grid.Height -= 24;
            }
        }

        protected void AddSpacer(MainWindow window)
        {
            var x = window.Grid;
            x.Height += 24;
            var row = new RowDefinition();
            row.Height = new GridLength(24, GridUnitType.Pixel);
            x.RowDefinitions.Add(row);
        }

        protected void AddRichTextbox(MainWindow window, string label, string var_name, bool enabled=true)
        {
            var x = window.Grid;
            x.Height += 60;
            var row = new RowDefinition();
            row.Height = new GridLength(60, GridUnitType.Pixel);
            x.RowDefinitions.Add(row);

            TextBlock txtBlock1 = new TextBlock();
            txtBlock1.Text = label;
            txtBlock1.FontSize = 14;
            txtBlock1.FontWeight = FontWeights.Bold;
            Grid.SetRow(txtBlock1, x.RowDefinitions.Count - 1);
            Grid.SetColumn(txtBlock1, 0);
            x.Children.Add(txtBlock1);


            TextBox tb = new TextBox();
            // disable drag and drop
            DataObject.AddCopyingHandler(tb, (sender, e) => { if (e.IsDragDrop) e.CancelCommand(); });
            Grid.SetRow(tb, x.RowDefinitions.Count - 1);
            Grid.SetColumn(tb, 1);
            tb.TextWrapping = TextWrapping.Wrap;
            tb.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            //tb.IsEnabled = enabled;
            tb.IsReadOnly = !enabled;
            tb.Text = (string)this.GetType().GetProperty(var_name).GetValue(this);
            tb.TextChanged += (sender, args) =>
            {
                window.ChangedFile = true;
                this.GetType().GetProperty(var_name).SetValue(this, tb.Text, null);
                UpdateData();
            };
            x.Children.Add(tb);
        }

        protected void AddDescriptionBox(MainWindow window)
        {
            var x = window.Grid;
            x.Height += 60;
            var row = new RowDefinition();
            row.Height = new GridLength(60, GridUnitType.Pixel);
            x.RowDefinitions.Add(row);

            TextBlock txtBlock1 = new TextBlock();
            txtBlock1.Text = "Description";
            txtBlock1.FontSize = 14;
            txtBlock1.FontWeight = FontWeights.Bold;
            Grid.SetRow(txtBlock1, x.RowDefinitions.Count - 1);
            Grid.SetColumn(txtBlock1, 0);
            x.Children.Add(txtBlock1);


            TextBox tb = new TextBox();
            // disable drag and drop
            DataObject.AddCopyingHandler(tb, (sender, e) => { if (e.IsDragDrop) e.CancelCommand(); });
            Grid.SetRow(tb, x.RowDefinitions.Count - 1);
            Grid.SetColumn(tb, 1);
            tb.TextWrapping = TextWrapping.Wrap;
            tb.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            tb.IsReadOnly = true;
            tb.Text = (string)this.GetType().GetProperty("Description").GetValue(this);
            x.Children.Add(tb);
        }

        protected void AddTextbox(MainWindow window, string label, string var_name, object obj= null)
        {
            if (obj == null)
                obj = this;

            var x = window.Grid;
            x.Height += 24;
            var row = new RowDefinition();
            row.Height = new GridLength(24, GridUnitType.Pixel);
            x.RowDefinitions.Add(row);

            TextBlock txtBlock1 = new TextBlock();
            txtBlock1.Text = label;
            txtBlock1.FontSize = 14;
            txtBlock1.FontWeight = FontWeights.Bold;
            Grid.SetRow(txtBlock1, x.RowDefinitions.Count - 1);
            Grid.SetColumn(txtBlock1, 0);
            x.Children.Add(txtBlock1);

            TextBox tb = new TextBox();
            // disable drag and drop
            DataObject.AddCopyingHandler(tb, (sender, e) => { if (e.IsDragDrop) e.CancelCommand(); });
            Grid.SetRow(tb, x.RowDefinitions.Count - 1);
            Grid.SetColumn(tb, 1);
            tb.Text = (string)obj.GetType().GetProperty(var_name).GetValue(obj);
            tb.TextChanged += (sender, args) =>
            {
                window.ChangedFile = true;
                obj.GetType().GetProperty(var_name).SetValue(obj, tb.Text, null);
                UpdateData();
            };
            x.Children.Add(tb);
        }

        protected void AddUint16(MainWindow window, string label, string var_name, object obj = null)
        {
            if (obj == null)
                obj = this;

            var x = window.Grid;
            x.Height += 24;
            var row = new RowDefinition();
            row.Height = new GridLength(24, GridUnitType.Pixel);
            x.RowDefinitions.Add(row);

            TextBlock txtBlock1 = new TextBlock();
            txtBlock1.Text = label;
            txtBlock1.FontSize = 14;
            txtBlock1.FontWeight = FontWeights.Bold;
            Grid.SetRow(txtBlock1, x.RowDefinitions.Count - 1);
            Grid.SetColumn(txtBlock1, 0);
            x.Children.Add(txtBlock1);

            IntegerUpDown ud = new IntegerUpDown();
            // disable drag and drop
            DataObject.AddCopyingHandler(ud, (sender, e) => { if (e.IsDragDrop) e.CancelCommand(); });
            ud.Background = BrushBackground;
            ud.Foreground = BrushForeground;
            Grid.SetRow(ud, x.RowDefinitions.Count - 1);
            Grid.SetColumn(ud, 1);
            ud.Width = 80;
            ud.HorizontalAlignment = HorizontalAlignment.Left;
            ud.Value = (UInt16)obj.GetType().GetProperty(var_name).GetValue(obj);
            ud.ValueChanged += (sender, args) =>
            {
                if (args.NewValue == null)
                    return;

                int number = (int)args.NewValue;
                
                if(number > UInt16.MaxValue) ud.Value = UInt16.MaxValue;
                else if (number < 0) ud.Value = 0;
                else
                {
                    window.ChangedFile = true;
                    obj.GetType().GetProperty(var_name).SetValue(obj, (UInt16)number, null);
                    UpdateData();
                }
            };
            x.Children.Add(ud);
        }

        protected void PopulateEntryList<T>(MainWindow window, List<T> entries, SelectionChangedEventHandler ev_handler)
        {
            window.EntriesList.ItemsSource = entries;
            window.EntriesList.SelectionChanged += ev_handler;
            if (entries.Count > 0)
                window.EntriesList.SelectedIndex = 0;
        }
    }
}
