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
        public const TokenType Type = 0;
        private SolidColorBrush BrushForeground = new SolidColorBrush(Color.FromRgb(255,255,255));
        private SolidColorBrush BrushBackground = new SolidColorBrush(Color.FromRgb(51, 51, 51));
        public List<MessagePointer> MessagePointerList = new List<MessagePointer>();

        public Token(DataWrapper dataWrapper, byte[] byteCommand, int offset)
        {
            _byteCommand = byteCommand;
            _opCode = byteCommand[0];
            _length = Convert.ToInt32(byteCommand[1]);
            _command = ((TokenType)((int)_opCode)).ToString();
            _offset = offset.ToString("X2");
            _dataWrapper = dataWrapper;
            Data = Utility.ToString(byteCommand);
        }


        public Token(DataWrapper dataWrapper, int offset)
        {
            _offset = offset.ToString("X2");
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
            _offset = "0x0";
            _dataWrapper = token._dataWrapper;
            Data = Utility.ToString(tempCommand);
        }
  
        protected DataWrapper _dataWrapper;

        protected byte[] _byteCommand;

        public byte[] ByteCommand
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

        protected string _offset;
        public string Offset
        {
            get => _offset;
            set => _offset = value;
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
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public virtual string GetMessages() { return null; }

        public virtual byte[] GetMessagesBytes() { return null; }

        public virtual int SetMessagePointer(int offset) { return offset; }

        public virtual void UpdateData() { }

        public virtual void UpdateGui(MainWindow window)
        {
            UpdateGui(window, true);
            AddTextbox(window, "Command", "Data");
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

        protected void AddCombobox<T>(MainWindow window, string label, string var_name)
        {
            var x = window.Grid;
            x.Height += 24;
            var row = new RowDefinition();
            row.Height = new GridLength(24, GridUnitType.Pixel);
            x.RowDefinitions.Add(row);

            TextBlock txtBlock1 = new TextBlock();
            txtBlock1.Text = label;
            txtBlock1.FontSize = 14;
            txtBlock1.FontWeight = FontWeights.Bold;
            Grid.SetRow(txtBlock1, x.RowDefinitions.Count-1);
            Grid.SetColumn(txtBlock1, 0);
            x.Children.Add(txtBlock1);

            ComboBox cb = new ComboBox();
            foreach (var e in Enum.GetValues(typeof(T)))
                cb.Items.Add(e);

            cb.SelectedItem = this.GetType().GetProperty(var_name).GetValue(this);
            Grid.SetRow(cb, x.RowDefinitions.Count - 1);
            Grid.SetColumn(cb, 1);
            cb.SelectionChanged += (sender, args) =>
            {
                this.GetType().GetProperty(var_name).SetValue(this, (T)args.AddedItems[0], null);
                UpdateData();
            };
            x.Children.Add(cb);
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

        protected void AddTranslationButton(MainWindow window, string label, string var_name, bool enabled = true)
        {
            var x = window.Grid;

            var row1 = new RowDefinition();
            row1.Height = new GridLength(24, GridUnitType.Pixel);
            x.Height += 24;
            x.RowDefinitions.Add(row1);

            TextBlock txtBlock1 = new TextBlock();
            txtBlock1.Text = label;
            txtBlock1.FontSize = 14;
            txtBlock1.FontWeight = FontWeights.Bold;
            Grid.SetRow(txtBlock1, x.RowDefinitions.Count - 1);
            Grid.SetColumn(txtBlock1, 0);
            x.Children.Add(txtBlock1);

            Button button_google = new Button();
            Grid.SetRow(button_google, x.RowDefinitions.Count - 1);
            Grid.SetColumn(button_google, 1);
            button_google.Content = "Google TL";
            x.Children.Add(button_google);

            var row3 = new RowDefinition();
            row3.Height = new GridLength(24, GridUnitType.Pixel);
            x.Height += 24;
            x.RowDefinitions.Add(row3);

            Button button_bing = new Button();
            Grid.SetRow(button_bing, x.RowDefinitions.Count - 1);
            Grid.SetColumn(button_bing, 1);
            button_bing.Content = "Bing TL";
            x.Children.Add(button_bing);

            var row4 = new RowDefinition();
            row4.Height = new GridLength(24, GridUnitType.Pixel);
            x.Height += 24;
            x.RowDefinitions.Add(row4);

            Button button_deepl = new Button();
            Grid.SetRow(button_deepl, x.RowDefinitions.Count - 1);
            Grid.SetColumn(button_deepl, 1);
            button_deepl.Content = "DeepL TL";
            x.Children.Add(button_deepl);


            var row2 = new RowDefinition();
            row2.Height = new GridLength(120, GridUnitType.Pixel);
            x.Height += 120;
            x.RowDefinitions.Add(row2);

            TextBox tb = new TextBox();
            // disable drag and drop
            DataObject.AddCopyingHandler(tb, (sender, e) => { if (e.IsDragDrop) e.CancelCommand(); });
            Grid.SetRow(tb, x.RowDefinitions.Count - 1);
            Grid.SetColumn(tb, 1);
            tb.TextWrapping = TextWrapping.Wrap;
            tb.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            tb.IsReadOnly = !enabled;
            x.Children.Add(tb);


            button_google.Click += (sender, args) =>
            {
                string input = (string)this.GetType().GetProperty(var_name).GetValue(this);

                string url = String.Format("https://translate.googleapis.com/translate_a/single?client=gtx&sl={0}&tl={1}&dt=t&q={2}", "ja", "en", Uri.EscapeUriString(input));
                WebClient webClient = new WebClient();
                webClient.Encoding = System.Text.Encoding.UTF8;
                webClient.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; " +
                                  "Windows NT 5.2; .NET CLR 1.0.3705;)");
                string result = webClient.DownloadString(url);

                // Get all json data
                var jsonData = new JavaScriptSerializer().Deserialize<List<dynamic>>(result);
                var translationItems = jsonData[0];
                string translation = "";

                // Loop through the collection extracting the translated objects
                foreach (object item in translationItems)
                {
                    IEnumerable translationLineObject = item as IEnumerable;
                    IEnumerator translationLineString = translationLineObject.GetEnumerator();
                    translationLineString.MoveNext();
                    translation += string.Format(" {0}", Convert.ToString(translationLineString.Current));
                }

                // Remove first blank character
                if (translation.Length > 1) { translation = translation.Substring(1); };

                // Return translation
                tb.Text = translation;
            };

            button_bing.Click += (sender, args) =>
            {
                string input = (string)this.GetType().GetProperty(var_name).GetValue(this);

                string url = "https://www.bing.com/ttranslatev3?isVertical=1&&IG=89A617AD83C84B9383CD52F1D13A4EB6&IID=translator.5026.3";
                WebClient webClient = new WebClient();
                webClient.Encoding = System.Text.Encoding.UTF8;
                webClient.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                string myParameters = String.Format("&fromLang={0}&to={1}&text={2}", "ja", "en", Uri.EscapeUriString(input));
                string result = webClient.UploadString(url, myParameters);

                // Get all json data
                var jsonData = new JavaScriptSerializer().Deserialize<List<dynamic>>(result);
                var translation = jsonData[0]["translations"][0]["text"];

                tb.Text = translation;
            };

            button_deepl.Click += (sender, args) =>
            {
                string input = (string)this.GetType().GetProperty(var_name).GetValue(this);

                string url = "https://www2.deepl.com/jsonrpc";

                var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    string json =
                        "{ \"jsonrpc\":\"2.0\"," +
                        "\"method\": \"LMT_handle_jobs\"," +
                        "\"params\":{ " +
                            "\"jobs\":[" +
                                "{ \"kind\":\"default\"," +
                                "\"raw_en_sentence\":\"" + input + "\"," +
                                "\"raw_en_context_before\":[]," +
                                "\"raw_en_context_after\":[]," +
                                "\"preferred_num_beams\":4}" +
                                "]," +
                            "\"lang\":{" +
                                "\"user_preferred_langs\":[\"EN\",\"DE\",\"JA\"]," +
                                "\"source_lang_user_selected\":\"JA\"," +
                                "\"target_lang\":\"EN\"" +
                                "}," +
                            "\"priority\":1," +
                            "\"commonJobParams\":{}," +
                            "\"timestamp\": " + (new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds()).ToString() +
                        "}," +
                        "\"id\":98910006" +
                        "}";

                    streamWriter.Write(json);
                }

                string result;
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    result = streamReader.ReadToEnd();
                }
                //string result = "{\"id\":98910006,\"jsonrpc\":\"2.0\",\"result\":{\"date\":\"20200510\",\"source_lang\":\"JA\",\"source_lang_is_confident\":1,\"target_lang\":\"EN\",\"timestamp\":1589154589,\"translations\":[{\"beams\":[{\"num_symbols\":18,\"postprocessed_sentence\":\"Where is this place, did Yuni survive, and who is this woman?\",\"score\":-4991.57,\"totalLogProb\":16.3437},{\"num_symbols\":18,\"postprocessed_sentence\":\"Where is this place, did Yuni survive, and who was this woman?\",\"score\":-4991.58,\"totalLogProb\":16.3246},{\"num_symbols\":18,\"postprocessed_sentence\":\"Where is this place, did Yuni survive, and who was that woman?\",\"score\":-4991.77,\"totalLogProb\":15.9421},{\"num_symbols\":19,\"postprocessed_sentence\":\"Where is this place, did Yuni survive, and who was this woman really?\",\"score\":-4992.6,\"totalLogProb\":14.3136}],\"quality\":\"normal\"}]}}\r\n";

                // Process JSON result
                result = result.Replace("\n", "").Replace("\r", "");
                var jsonData = new JavaScriptSerializer().Deserialize<dynamic>(result);
                var v = jsonData["result"];

                var final = "";
                foreach (var beam in jsonData["result"]["translations"][0]["beams"])
                {
                    if (final != "")
                        final += "\n\n";
                    final += beam["postprocessed_sentence"];
                }

                tb.Text = final;
            };
        }



        protected void AddUint8(MainWindow window, string label, string var_name, object obj=null)
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

            ByteUpDown ud = new ByteUpDown();
            // disable drag and drop
            DataObject.AddCopyingHandler(ud, (sender, e) => { if (e.IsDragDrop) e.CancelCommand(); });
            var style = Application.Current.Resources[typeof(Button)];
            ud.Background = BrushBackground;
            ud.Foreground = BrushForeground;
            Grid.SetRow(ud, x.RowDefinitions.Count - 1);
            Grid.SetColumn(ud, 1);
            ud.Width = 60;
            ud.HorizontalAlignment = HorizontalAlignment.Left;
            ud.Value = (byte)obj.GetType().GetProperty(var_name).GetValue(obj);
            ud.ValueChanged += (sender, args) =>
            {
                window.ChangedFile = true;
                obj.GetType().GetProperty(var_name).SetValue(obj, (byte)args.NewValue, null);
                UpdateData();
            };
            x.Children.Add(ud);
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

    public enum TokenType
    {
        ext_Goto = 0x09,
        ext_Call = 0x0A,
        ext_Goto2 = 0x0B,
        ext_Call2 = 0x0C,
        ret2 = 0x0D,
        thread = 0x0E,
        skip_jump = 0x14,
        key_wait = 0x15,
        message = 0x18,
        windows = 0x1A,
        select = 0x1B,
        selectP = 0x1C,
        select2 = 0x1D,
        popup = 0x1E,
        mes_sync = 0x1F,
        mes_log = 0x24,
        scr_mode = 0x20,
        set_save_point = 0x21,
        clear_save_point = 0x22,
        set_prv_point = 0x23,
        auto_start = 0x25,
        auto_stop = 0x26,
        quick_Save = 0x27,
        mes_log_save = 0xF8,
        title_display = 0x28,
        location_display = 0x2B,
        date_display = 0x29,
        get_options = 0x2C,
        set_icon = 0x2D,
        menu_enable = 0x2E,
        menu_disable = 0x2F,
        fade_out = 0x30,
        fade_in = 0x31,
        fade_out_start = 0x32,
        fade_out_stop = 0x33,
        fade_wait = 0x34,
        fade_pri = 0x36,
        filt_in = 0x38,
        filt_out = 0x39,
        filt_out_start = 0x3B,
        filt_in_start = 0x3A,
        filt_wait = 0x3C,
        filt_pri = 0x3E,
        char_init = 0x40,
        char_display = 0x42,
        char_ers = 0x43,
        char_no = 0x50,
        char_on = 0x51,
        char_pri = 0x52,
        char_animation = 0x53,
        char_sort = 0x54,
        char_swap = 0x55,
        char_shadow = 0x56,
        char_ret = 0x57,
        char_attack = 0x59,
        get_background_c = 0x5F,
        obj_ini = 0x60,
        obj_display = 0x62,
        obj_erase = 0x63,
        obj_no = 0x70,
        obj_on = 0x71,
        obj_pri = 0x72,
        obj_animation = 0x73,
        obj_sort = 0x74,
        obj_swap = 0x75,
        face_ini = 0x80,
        face_display = 0x82,
        face_erase = 0x83,
        face_pos = 0x84,
        face_auto_pis = 0x85,
        face_no = 0x88,
        face_on = 0x89,
        face_pri = 0x8A,
        face_animation = 0x8B,
        face_shadow = 0x8E,
        face_ret = 0x8F,
        bg_init = 0x90,
        bg_display = 0x91,
        bg_erase = 0x92,
        bg_flag = 0x93,
        bg_on = 0x9B,
        bg_pri = 0x9C,
        bg_att = 0x9D,
        bg_bnk = 0x9E,
        bg_swap = 0x9F,
        effect_start = 0xA1,
        effect_par = 0xA2,
        effect_stop = 0xA3,
        sound_effect_play = 0xC8,
        sound_effect_start = 0xC9,
        sound_effect_stop = 0xCA,
        sound_effect_wait = 0xCB,
        sound_effect_vol = 0xCC,
        s_sound_effect_start = 0xCE,
        voice_over_play = 0xD0,
        voice_over_start = 0xD1,
        voice_over_stop = 0xD2,
        voice_over_wait = 0xD3,
        voice_over_sts = 0xD4, 
        moive_play = 0xD8,
        movie_start = 0xD9,
        move_stop_0xDA,
        move_wait = 0xDB,
        key_start = 0xDC,
        vibration_start = 0xE0,
        vibration_stop = 0xE1,
        screen_calen_start = 0xE2,
        screen_calen_end = 0xE3,
        RT = 0xE4,
        print = 0xF0,
        debug_set = 0xF1,
        debug_get = 0xF2,
        title_on = 0xF3,
        dict_set = 0xF4,
        dict_flag = 0xF5,
        message_flag = 0xF6,
        sel_flag = 0xF7,
        set_back_col = 0xF9,
        set_name = 0xFA,
        Tlst_call_on = 0xFB,
        set_thum = 0xFC,
        eot = 0xFE,
        eos = 0xFF,
        event_ini = 0xB0,
        event_release = 0xB1,
        event_open = 0xB3,
        event_close = 0xB4,
        event_load = 0xB2,
        event_key_wait = 0xB5,
        Rain_Effect = 0xA1,
        Loop_Cond = 0x06,
    }
}
