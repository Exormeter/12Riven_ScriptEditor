﻿using System;
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

    

    public class Token : INotifyPropertyChanged
    {
        private SolidColorBrush BrushForeground = new SolidColorBrush(Color.FromRgb(0,0,0));
        private SolidColorBrush BrushBackground = new SolidColorBrush(Color.FromRgb(1, 1, 1));
        public List<MessagePointer> MessagePointerList = new List<MessagePointer>();

        public Token(DataWrapper dataWrapper, byte[] byteCommand, int offset)
        {
            _byteCommand = byteCommand;
            _opCode = byteCommand[0];
            _length = ScriptTokenizer.OpcodeList[byteCommand[0]].length;
            _command = ScriptTokenizer.OpcodeList[byteCommand[0]].name;
            _offset = offset;
            _dataWrapper = dataWrapper;
            Data = Utility.ToString(byteCommand);
        }


        public Token(DataWrapper dataWrapper, int offset)
        {
            _offset = offset;
            _dataWrapper = dataWrapper;
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
