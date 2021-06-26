using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using Microsoft.WindowsAPICodePack.Dialogs;

using Riven_Script_Editor.Tokens;
using Riven_Script_Editor.FileTypes;
using System.Configuration;
using System.Text.RegularExpressions;

namespace Riven_Script_Editor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ObservableCollection<ListViewItem> lvList = new ObservableCollection<ListViewItem>();
        string folder = "";
        string filename = "";
        bool searchEndOfFile = false;
        readonly Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        ATokenizer Tokenizer;
        private List<Token> tokenList;
        Token clipBoardToken = null;

        public Grid Grid;
        public ListBox EntriesList;

        public bool ChangedFile { get; internal set; }

        public MainWindow()
        {
            InitializeComponent();
            Grid = ((MainWindow)Application.Current.MainWindow).GuiArea;
            EntriesList = ((MainWindow)Application.Current.MainWindow).listviewEntries;
            listviewFiles.ItemsSource = lvList;
            this.Closing += MainWindow_Closing;

            textbox_inputFolder.Text = GetConfig("input_folder");
            //textbox_inputFolderJp.Text = GetConfig("input_folder_jp");
            textbox_listFile.Text = GetConfig("list_file");
            textbox_exportedAfs.Text = GetConfig("exported_afs");
            checkbox_SearchCaseSensitive.IsChecked = GetConfig("case_sensitive") == "1";
            checkbox_SearchAllFiles.IsChecked = GetConfig("search_all_files") == "1";
            textbox_search.Text = GetConfig("last_search");

            MenuViewFolder.IsChecked = GetConfig("view_folders", "1") == "1";
            if (!(bool)MenuViewFolder.IsChecked)
                GridTextboxes.Visibility = Visibility.Collapsed;
            MenuViewDescription.IsChecked = GetConfig("view_description", "1") == "1";
            MenuViewLabel.IsChecked = GetConfig("view_label", "1") == "1";

            textbox_inputFolder.TextChanged += (sender, ev) => { UpdateConfig("input_folder", textbox_inputFolder.Text); BrowseInputFolder(null, null); };
            //textbox_inputFolderJp.TextChanged += (sender, ev) => UpdateConfig("input_folder_jp", textbox_inputFolderJp.Text);;
            textbox_listFile.TextChanged += (sender, ev) => UpdateConfig("list_file", textbox_listFile.Text);
            textbox_exportedAfs.TextChanged += (sender, ev) => UpdateConfig("exported_afs", textbox_exportedAfs.Text);
            checkbox_SearchCaseSensitive.Checked += (sender, ev) => UpdateConfig("case_sensitive", "1");
            checkbox_SearchCaseSensitive.Unchecked += (sender, ev) => UpdateConfig("case_sensitive", "0");
            checkbox_SearchAllFiles.Checked += (sender, ev) => UpdateConfig("search_all_files", "1");
            checkbox_SearchAllFiles.Unchecked += (sender, ev) => UpdateConfig("search_all_files", "0");
            textbox_search.TextChanged += (sender, ev) => UpdateConfig("last_search", textbox_search.Text);
            textbox_search.KeyDown += Textbox_search_KeyDown;
            BrowseInputFolder(null, null);

            AddHotKeys();
            Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            bool success = CheckUnsavedChanges();
            e.Cancel = !success;
        }
        
        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            var x = new ExceptionPopup(e.Exception);
            x.ShowDialog();
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.ExceptionObject.ToString());
        }

        private bool CheckUnsavedChanges()
        {
            // Check for file changes, then prompt user to save
            if (ChangedFile)
            {
                MessageBoxResult dialogResult = MessageBox.Show("File changed. Save?", "Unsaved changes", MessageBoxButton.YesNoCancel);

                if (dialogResult == MessageBoxResult.Yes)
                {
                    string outPath = System.IO.Path.Combine(folder, filename);
                    byte[] output = Tokenizer.AssembleAsData();
                    var stream_out = new FileStream(outPath, FileMode.Create, FileAccess.ReadWrite);
                    stream_out.Write(output, 0, output.Length);
                    stream_out.Close();
                }
                else if (dialogResult == MessageBoxResult.Cancel)
                    return false;

                ChangedFile = false;
            }
            return true;
        }

        private void Textbox_search_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SearchNext(null, null);
        }

        private string GetConfig(string key, string initial = "")
        {
            if (config.AppSettings.Settings.AllKeys.Contains(key))
                return config.AppSettings.Settings[key].Value;

            return initial;
        }

        private void UpdateConfig(string key, string value)
        {
            
            config.AppSettings.Settings.Remove(key);
            config.AppSettings.Settings.Add(key, value);
            config.Save();
        }

        private void AddHotKeys()
        {
            RoutedCommand com;

            com = new RoutedCommand();
            com.InputGestures.Add(new KeyGesture(Key.S, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(com, Menu_File_Save));

            com = new RoutedCommand();
            com.InputGestures.Add(new KeyGesture(Key.F, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(com, SearchFocus));

            com = new RoutedCommand();
            com.InputGestures.Add(new KeyGesture(Key.E, ModifierKeys.Control));
            CommandBindings.Add(new CommandBinding(com, Menu_Export_Mac));

            com = new RoutedCommand();
            com.InputGestures.Add(new KeyGesture(Key.F3, ModifierKeys.None));
            CommandBindings.Add(new CommandBinding(com, SearchNext));

            com = new RoutedCommand();
            com.InputGestures.Add(new KeyGesture(Key.F4, ModifierKeys.None));
            CommandBindings.Add(new CommandBinding(com, SearchPrev));

            com = new RoutedCommand();
            com.InputGestures.Add(new KeyGesture(Key.F5, ModifierKeys.None));
            CommandBindings.Add(new CommandBinding(com, FocusTextNext));

            com = new RoutedCommand();
            com.InputGestures.Add(new KeyGesture(Key.F6, ModifierKeys.None));
            CommandBindings.Add(new CommandBinding(com, FocusTextPrev));
        }

        private void BrowseInputFolder(object sender, RoutedEventArgs e)
        {
            if (sender == null)
            {
                folder = textbox_inputFolder.Text;
                if (!Directory.Exists(folder))
                {
                    lvList.Clear();
                    return;
                }
            }

            else
            {
                CommonOpenFileDialog dialog = new CommonOpenFileDialog();
                dialog.IsFolderPicker = true;
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                    textbox_inputFolder.Text = dialog.FileName;
                return; // Return because changing the textbox will trigger this function again
            }
            
            lvList.Clear();
            Regex sceneNamePattern = new Regex("^([A-z]{2}[0-9]{2}).*");
            // Populate the list

            string[] filepaths = Directory.GetFiles(folder, "*.BIN", SearchOption.TopDirectoryOnly);
            
            foreach (var filePath in filepaths)
            {
                string filename = System.IO.Path.GetFileName(filePath);
                if (filename.Equals("DATA.BIN") || filename.Equals("Repi.BIN") || sceneNamePattern.IsMatch(filename))
                {
                    lvList.Add(new ListViewItem() { Content = System.IO.Path.GetFileName(filename) });
                }
                
            }
                

            // Clicking the file name will load the file
            listviewFiles.SelectionChanged += ListViewFiles_SelectionChanged;
        }

        //private void BrowseInputFolderJp(object sender, RoutedEventArgs e)
        //{
        //    CommonOpenFileDialog dialog = new CommonOpenFileDialog();
        //    dialog.IsFolderPicker = true;
        //    if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
        //        textbox_inputFolderJp.Text = dialog.FileName;
        //}

        private void BrowseFilelist(object sender, RoutedEventArgs e) {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                textbox_listFile.Text = dialog.FileName;
        }

        private void BrowseExportedAfs(object sender, RoutedEventArgs e) {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                textbox_exportedAfs.Text = dialog.FileName;
        }

        private void SearchFocus(object sender, RoutedEventArgs e)
        {
            textbox_search.Focus();
        }

        private void SearchNext(object sender, RoutedEventArgs e)
        {
            Search(true);
        }

        private void SearchPrev(object sender, RoutedEventArgs e)
        {
            Search(false);
        }

        private void DeleteNode(object sender, RoutedEventArgs e)
        {
            if (TokenListView.SelectedIndex > -1)
            {
                int idx = TokenListView.SelectedIndex;
                TokenListView.SelectedIndex += 1;

                tokenList.RemoveAt(idx);
                CommandViewBox vb = DataContext as CommandViewBox;
                vb.MyListItems.RemoveAt(idx);
            }
        }

        private void CopyNode(object sender, RoutedEventArgs e)
        {
            // @TODO
            if (TokenListView.SelectedIndex > -1)
            {
                clipBoardToken = tokenList[TokenListView.SelectedIndex];
            }

        }


        private void InsertNode(object sender, RoutedEventArgs e)
        {
            // @TODO
            if (TokenListView.SelectedIndex > -1 && clipBoardToken != null)
            {
                int idx = TokenListView.SelectedIndex + 1;
                tokenList.Insert(idx, clipBoardToken);
                CommandViewBox vb = DataContext as CommandViewBox;
                vb.MyListItems.Insert(idx, clipBoardToken.Clone());
                TokenListView.SelectedIndex += 1;
            }


        }
        private void Search(bool next)
        {
            if (textbox_search.Text == "")
                return;

            int mod(int k, int n) { return ((k %= n) < 0) ? k + n : k; }

            if ((bool)checkbox_SearchAllFiles.IsChecked)
            {
                int startIdx = listviewFiles.SelectedIndex;
                if (startIdx == -1) startIdx = 0;

                int idx = startIdx;

                while (!SearchToken(textbox_search.Text, next, (bool)checkbox_SearchCaseSensitive.IsChecked))
                {
                    bool success = CheckUnsavedChanges();
                    if (!success)
                        return;

                    if (next)
                        idx = mod(idx + 1, listviewFiles.Items.Count);
                    else
                        idx = mod(idx - 1, listviewFiles.Items.Count);
                    
                    if (idx == startIdx)
                    {
                        MessageBox.Show("Searched all files");
                        break;
                    }

                    // Select and focus on the file
                    listviewFiles.SelectedIndex = idx;
                    listviewFiles.UpdateLayout();
                    listviewFiles.ScrollIntoView(listviewFiles.Items[idx]);
                }
            }
            else
            {
                if (SearchToken(textbox_search.Text, next, (bool)checkbox_SearchCaseSensitive.IsChecked))
                    MessageBox.Show("End of file");
            }
        }

        public bool SearchToken(string text, bool next, bool case_sensitive)
        {
            int idx = TokenListView.SelectedIndex;
            if (idx == -1)
                idx = 0;

            text = Utility.StringDoubleSpace(text);
            if (!case_sensitive)
                text = text.ToLower();

            if (searchEndOfFile)
            {
                if (next) idx = 0;
                else idx = TokenListView.Items.Count - 1;
                searchEndOfFile = false;
            }

            while (true)
            {
                if (next)
                {
                    idx++;
                    if (idx >= TokenListView.Items.Count - 1) break;
                }
                else
                {
                    idx--;
                    if (idx < 0) break;
                }

                object t = TokenListView.Items[idx];
                string msg = (t as Token).GetMessages();

                if (msg == null) continue;

                if (!case_sensitive)
                    msg = msg.ToLower();

                if (msg.Contains(text))
                {
                    // Select and focus on the token
                    TokenListView.SelectedIndex = idx;
                    TokenListView.UpdateLayout();
                    TokenListView.ScrollIntoView(TokenListView.Items[idx]);
                    return true;
                }
            }

            searchEndOfFile = true;
            //MessageBox.Show("End of file");
            return false;
        }

        private void FocusTextNext(object sender, RoutedEventArgs e)
        {
            
            int idx = TokenListView.SelectedIndex;
            if (idx == -1)
                idx = 0;

            while (true)
            {
                if (true)
                {
                    idx++;
                    if (idx >= TokenListView.Items.Count - 1) break;
                }
                else
                {
                    idx--;
                    if (idx < 0) break;
                }

                object t = TokenListView.Items[idx];
                string msg = (t as Token).GetMessages();

                if (msg != null)
                {
                    // Select and focus on the token
                    TokenListView.SelectedIndex = idx;
                    TokenListView.UpdateLayout();
                    TokenListView.ScrollIntoView(TokenListView.Items[idx]);

                    // Focus the message field
                    if (t is TokenMsgDisp2)
                        GetGridAtPos(4, 1).Focus();
                    //else if (t is TokenSystemMsg)
                    //    GetGridAtPos(3, 1).Focus();
                    else if (t is TokenSelectDisp2)
                        GetGridAtPos(3, 1).Focus();

                    return;
                }
            }

            MessageBox.Show("End of file. No more text.");
           
        }

        UIElement GetGridAtPos(int row, int col)
        {
            foreach (UIElement e in Grid.Children)
            {
                if (Grid.GetRow(e) == row && Grid.GetColumn(e) == col)
                    return e;
            }
            return null;
        }

        public bool Search(string text, bool next, bool case_sensitive)
        {
            int idx = TokenListView.SelectedIndex;
            if (idx == -1)
                idx = 0;

            text = Utility.StringDoubleSpace(text);
            if (!case_sensitive)
                text = text.ToLower();

            if (searchEndOfFile)
            {
                if (next) idx = 0;
                else idx = TokenListView.Items.Count - 1;
                searchEndOfFile = false;
            }

            while (true)
            {
                if (next)
                {
                    idx++;
                    if (idx >= TokenListView.Items.Count - 1) break;
                }
                else
                {
                    idx--;
                    if (idx < 0) break;
                }

                object t = TokenListView.Items[idx];
                string msg = (t as Token).GetMessages();

                if (msg == null) continue;

                if (!case_sensitive)
                    msg = msg.ToLower();

                if (msg.Contains(text))
                {
                    // Select and focus on the token
                    TokenListView.SelectedIndex = idx;
                    TokenListView.UpdateLayout();
                    TokenListView.ScrollIntoView(TokenListView.Items[idx]);
                    return true;
                }
            }

            searchEndOfFile = true;
            //MessageBox.Show("End of file");
            return false;
        }


        private void MenuViewFolders_Clicked(object sender, RoutedEventArgs e)
        {
            MenuViewFolder.IsChecked = !MenuViewFolder.IsChecked;
            UpdateConfig("view_folders", MenuViewFolder.IsChecked ? "1": "0");
            GridTextboxes.Visibility = MenuViewFolder.IsChecked ? Visibility.Visible : Visibility.Collapsed;
        }

        private void MenuViewDescription_Clicked(object sender, RoutedEventArgs e)
        {
            MenuViewDescription.IsChecked = !MenuViewDescription.IsChecked;
            UpdateConfig("view_description", MenuViewDescription.IsChecked ? "1" : "0");

            if (TokenListView.SelectedItem != null)
                (TokenListView.SelectedItem as Token).UpdateGui(this);
        }

        private void MenuViewLabel_Clicked(object sender, RoutedEventArgs e)
        {
            MenuViewLabel.IsChecked = !MenuViewLabel.IsChecked;
            UpdateConfig("view_label", MenuViewLabel.IsChecked ? "1" : "0");

            if (TokenListView.SelectedItem != null)
                (TokenListView.SelectedItem as Token).UpdateGui(this);
        }
        
        private void FocusTextPrev(object sender, RoutedEventArgs e)
        {
            //GoToNextText(false);
        }
        
        private void Menu_File_Save(object sender, RoutedEventArgs e)
        {
            string fname;
            try
            {
                fname = (string)(listviewFiles.SelectedItem as ListViewItem).Content;
            } catch { return;  }


            try
            {
                string outPath = System.IO.Path.Combine(folder, (string)(listviewFiles.SelectedItem as ListViewItem).Content);
                byte[] output = Tokenizer.AssembleAsData();
                var stream_out = new FileStream(outPath, FileMode.Create, FileAccess.ReadWrite);
                stream_out.Write(output, 0, output.Length);
                stream_out.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Menu_Export_Mac(object sender, RoutedEventArgs e)
        {
            if (textbox_exportedAfs.Text == "")
                return;

            var stream = new FileStream(textbox_exportedAfs.Text, FileMode.Create, FileAccess.Write);
            try
            {
                byte[] data = AFS.Pack(textbox_listFile.Text, textbox_inputFolder.Text);
                stream.Write(data, 0, (int)data.Length);
                stream.Close();
                MessageBox.Show("Exported " + textbox_exportedAfs.Text);
            }
            catch (Exception ex)
            {
                stream.Close();
                MessageBox.Show(ex.Message, "Error exporting AFS");
            }
        }

        private void Menu_Export_Txt(object sender, RoutedEventArgs e)
        {
            string fname;
            try
            {
                fname = (string)(listviewFiles.SelectedItem as ListViewItem).Content;
            }
            catch { return; }


            try
            {
                string txt_filename = System.IO.Path.Combine(folder, (string)(listviewFiles.SelectedItem as ListViewItem).Content + ".txt");
                var stream_out = new FileStream(txt_filename, FileMode.Create, FileAccess.ReadWrite);
                byte[] output = Tokenizer.AssembleAsText((string)(listviewFiles.SelectedItem as ListViewItem).Content);
                stream_out.Write(output, 0, output.Length);
                stream_out.Close();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ListView1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
                return;
            var t = e.AddedItems[0];
            
            (t as Token).UpdateGui(this);
        }

        private void ListViewFiles_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            if (args.AddedItems.Count == 0)
                return;

            var success = CheckUnsavedChanges();
            if (!success)
                return;

            filename = (string)((ListViewItem)args.AddedItems[0]).Content;
            string path_en = System.IO.Path.Combine(folder, filename);

            
            byte[] binData = File.ReadAllBytes(path_en);
            if(filename.Equals("DATA.BIN"))
            {
                Tokenizer = new DataTokenizer(new DataWrapper(binData));
            }
            else
            {
                Tokenizer = new ScriptTokenizer(new DataWrapper(binData));
            }
            tokenList = Tokenizer.ParseData();
            ((MainWindow)Application.Current.MainWindow).Title = "12R Script: " + filename;
            DataContext = new CommandViewBox(tokenList);
        }

        private void ListView1_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            //ContextMenu m = new ContextMenu();
            //var token = (sender as ListViewItem).Content;
            //if (token is TokenIf)
            //{
            //    var label = (token as TokenIf).LabelJump;
            //    var v = new MenuItem() { Header = "Jump to " + label };
            //    v.Click += (sender2, arg) => { Tokenizer.JumpToLabel(label); };
            //    m.Items.Add(v);
            //}
            //else if (token is TokenInternalGoto)
            //{
            //    var label = (token as TokenInternalGoto).LabelJump;
            //    var v = new MenuItem() { Header = "Jump to " + label };
            //    v.Click += (sender2, arg) => { Tokenizer.JumpToLabel(label); };
            //    m.Items.Add(v);
            //}
            //else if (token is TokenMsgDisp2)
            //{
            //    var labels = (token as TokenMsgDisp2).IdenticalJpLabels;
            //    foreach (var label in labels)
            //    {
            //        var v = new MenuItem() { Header = "Identical to " + label };
            //        v.Click += (sender2, arg) => { Tokenizer.JumpToLabel(label); };
            //        m.Items.Add(v);
            //    }
            //}
            //else if (token is TokenSelectDisp2)
            //{
            //    var labels = (token as TokenSelectDisp2).IdenticalJpLabels;
            //    foreach (var label in labels)
            //    {
            //        var v = new MenuItem() { Header = "Identical to " + label };
            //        v.Click += (sender2, arg) => { Tokenizer.JumpToLabel(label); };
            //        m.Items.Add(v);
            //    }
            //}

            //foreach (var reflabel in (token as Token).ReferingLabels)
            //{
            //    var v = new MenuItem() { Header = "Jumped here from " + reflabel };
            //    v.Click += (sender2, arg) => { Tokenizer.JumpToLabel(reflabel); };
            //    m.Items.Add(v);
            //}
            ///*
            //else if (token is TokenMsgDisp2)
            //{
            //    var label = (token as TokenMsgDisp2).LabelJump;
            //    var v = new MenuItem() { Header = "Jump to " + label };
            //    v.Click += (sender2, arg) => { Tokenizer.JumpToLabel(label); };
            //    m.Items.Add(v);
            //}
            //*/

            //if (m.Items.Count > 0)
            //    ListView1.ContextMenu = m;
            //else
            //    ListView1.ContextMenu = null;
        }
    }

    public class CommandViewBox : INotifyPropertyChanged
    {
        private ObservableCollection<Token> _myListItems;
        public ObservableCollection<Token> MyListItems
        {
            get => _myListItems;
            set
            {
                _myListItems = value;
                OnPropertyChanged(nameof(MyListItems));
            }
        }

        public CommandViewBox(List<Token> tokenList)
        {
            MyListItems = new ObservableCollection<Token>();

            foreach (var token in tokenList)
            {
                MyListItems.Add(token);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class TokenSelectorComboBoxItem
    {
        public string Name { get; set; }
        public Type Value { get; set; }
        public override string ToString() { return this.Name; }

        public TokenSelectorComboBoxItem(string text)
        {
            this.Name = text;
            this.Value = Type.GetType("R11_Script_Editor.Tokens.Token" + text);
        }
    }
}