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
        string splittedFilenameEnding = "_continued";
        bool searchEndOfFile = false;
        readonly Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        ATokenizer Tokenizer;
        private List<Token> tokenList;

        public Grid Grid;
        public ListBox EntriesList;
        public TextBlock ScriptSizeTextBlock;

        public ScriptSizeNotifier scriptSizeNotifier;
        public ScriptListFileManager scriptListFileManager;

        public bool ChangedFile { get; internal set; }

        public MainWindow()
        {
            InitializeComponent();

            scriptListFileManager = new ScriptListFileManager(GetConfig("list_file"));
            Grid = ((MainWindow)Application.Current.MainWindow).GuiArea;
            EntriesList = ((MainWindow)Application.Current.MainWindow).listviewEntries;
            ScriptSizeTextBlock = ((MainWindow)Application.Current.MainWindow).ScriptSizeCounter;

            listviewFiles.DataContext = scriptListFileManager;
            listviewFiles.SelectionChanged += ListViewFiles_SelectionChanged;

            this.Closing += MainWindow_Closing;

            textbox_inputFolder.Text = GetConfig("input_folder");
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
            textbox_listFile.TextChanged += (sender, ev) => { UpdateConfig("list_file", textbox_listFile.Text); LoadScriptList(textbox_listFile.Text); };
            textbox_exportedAfs.TextChanged += (sender, ev) => UpdateConfig("exported_afs", textbox_exportedAfs.Text);
            checkbox_SearchCaseSensitive.Checked += (sender, ev) => UpdateConfig("case_sensitive", "1");
            checkbox_SearchCaseSensitive.Unchecked += (sender, ev) => UpdateConfig("case_sensitive", "0");
            checkbox_SearchAllFiles.Checked += (sender, ev) => UpdateConfig("search_all_files", "1");
            checkbox_SearchAllFiles.Unchecked += (sender, ev) => UpdateConfig("search_all_files", "0");
            textbox_search.TextChanged += (sender, ev) => UpdateConfig("last_search", textbox_search.Text);
            textbox_search.KeyDown += Textbox_search_KeyDown;
            BrowseInputFolder(null, null);
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
                    byte[] output = Tokenizer.AssembleAsData(tokenList);
                    if (!SaveFile(filename, output)) return false;
                }
                else if (dialogResult == MessageBoxResult.Cancel)
                    return false;
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

        private void BrowseInputFolder(object sender, RoutedEventArgs e)
        {
            if (sender == null)
            {
                folder = textbox_inputFolder.Text;
                return;
            }

            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                textbox_inputFolder.Text = dialog.FileName;
        }

        private void LoadScriptList(string filepath)
        {
            scriptListFileManager.Load(filepath);
        }

        private void BrowseFilelist(object sender, RoutedEventArgs e) {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                textbox_listFile.Text = dialog.FileName;
            }
                

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
        
        private void Menu_File_Save(object sender, RoutedEventArgs e)
        {
            if (filename == "") return;

            byte[] output = Tokenizer.AssembleAsData(tokenList.ToList());
            SaveFile(filename, output);
        }

        private bool SaveFile(string fileName, byte[] data)
        {
            if (data.Length > 0xFFFF)
            {
                MessageBox.Show("Please split the script before saving it", "Script length exceeded");
                return false;
            }
            try
            {
                string outPath = System.IO.Path.Combine(folder, fileName);
                
                var stream_out = new FileStream(outPath, FileMode.Create, FileAccess.ReadWrite);
                stream_out.Write(data, 0, data.Length);
                stream_out.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
            ChangedFile = false;
            return true;

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
            string fileName;
            try
            {
                fileName = (string)listviewFiles.SelectedItem;
                byte[] output = Tokenizer.AssembleAsText(fileName, tokenList.ToList());
                SaveFile(fileName + ".txt", output);
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

            filename = (string)args.AddedItems[0]; 
            string path_en = System.IO.Path.Combine(folder, filename);

            
            byte[] binData = File.ReadAllBytes(path_en);
            if(filename.Equals("DATA.BIN"))
            {
                Tokenizer = new DataTokenizer(new DataWrapper(binData));
            }
            else
            {
                Tokenizer = new ScriptTokenizer(new DataWrapper(binData), scriptListFileManager);
            }
            tokenList = Tokenizer.ParseData();
            ScriptSizeCounter.DataContext = new ScriptSizeNotifier(tokenList);
            ((MainWindow)Application.Current.MainWindow).Title = "12R Script: " + filename;
            DataContext = new CommandViewBox(tokenList.ToList());
        }

        private void ListView1_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            ContextMenu contextMenu = new ContextMenu();

            MenuItem menuItemSplit = new MenuItem();
            menuItemSplit.Header = "Split Script Here";
            string scriptName = (string)listviewFiles.SelectedItem;
            menuItemSplit.IsEnabled = !scriptName.Contains(splittedFilenameEnding) && (tokenList[TokenListView.SelectedIndex].Splitable != "No");
            menuItemSplit.Click += new RoutedEventHandler(ScriptSplitContextMenu_MouseUp);
            contextMenu.Items.Add(menuItemSplit);

            if (tokenList[TokenListView.SelectedIndex].OpCode == 0x0B)
            {
                MenuItem menuItemJoin = new MenuItem();
                menuItemJoin.Header = "Join Scripts";
                menuItemJoin.Click += new RoutedEventHandler(JoinTokens_MouseUp);
                contextMenu.Items.Add(menuItemJoin);
            }

            TokenListView.ContextMenu = contextMenu;
        }

        private void JoinTokens_MouseUp(Object sender, System.EventArgs e)
        {
            TokenExtGoto token = (TokenExtGoto)tokenList[TokenListView.SelectedIndex];
            FixExGotoIndexes(scriptListFileManager.getFilenameIndex(token.referencedFilename));
            byte[] binData = File.ReadAllBytes(System.IO.Path.Combine(folder, token.referencedFilename));
            scriptListFileManager.RemoveFilename(token.referencedFilename);
            ScriptTokenizer scriptTokenizer = new ScriptTokenizer(new DataWrapper(binData), scriptListFileManager);
            var breakoutTokenList = scriptTokenizer.ParseData();
            breakoutTokenList.RemoveAt(0); //remove header
            tokenList.RemoveAt(tokenList.Count - 1); //remove trailer
            tokenList.RemoveAt(tokenList.Count - 1); //remove end script opcode
            tokenList.RemoveAt(tokenList.Count - 1); //remove goto opcode
            tokenList.AddRange(breakoutTokenList);
            SaveFile((string)listviewFiles.SelectedItem, Tokenizer.AssembleAsData(tokenList));
            DataContext = new CommandViewBox(tokenList);
            ScriptSizeCounter.DataContext = new ScriptSizeNotifier(tokenList);
            File.Delete(folder + "\\" + token.referencedFilename);
        }

        private void ScriptSplitContextMenu_MouseUp(Object sender, System.EventArgs e) {
            string breakoutScriptName = (string)listviewFiles.SelectedItem;
            string[] scriptNameParts = breakoutScriptName.Split('.');
            breakoutScriptName = scriptNameParts[0] + splittedFilenameEnding + "." + scriptNameParts[1];
            byte scriptIndex = (byte)scriptListFileManager.AddFilename(breakoutScriptName);
            var breakoutTokenList = tokenList.GetRange(TokenListView.SelectedIndex + 1, tokenList.Count - TokenListView.SelectedIndex - 1);
            breakoutTokenList.Insert(0, tokenList[0]); //add copied header
            var commandBytes = new byte[] {0x0B, 0x06, scriptIndex, 0x00 , 0x00, 0x00};
            var callExtToken = new TokenExtGoto(null, commandBytes, 0, breakoutScriptName);
            tokenList.Insert(TokenListView.SelectedIndex + 1, callExtToken);
            tokenList.RemoveRange(TokenListView.SelectedIndex + 2, tokenList.Count() - TokenListView.SelectedIndex - 4);
            DataContext = new CommandViewBox(tokenList);
            ScriptSizeCounter.DataContext = new ScriptSizeNotifier(tokenList);
            SaveFile(breakoutScriptName, Tokenizer.AssembleAsData(breakoutTokenList));
            SaveFile((string)listviewFiles.SelectedItem, Tokenizer.AssembleAsData(tokenList));
        }

        private void FixExGotoIndexes(int removedIndex)
        {
            foreach (string filename in scriptListFileManager.ScriptFilenameList)
            {
                if (filename.Equals("DATA.BIN")) continue;
                string path_en = System.IO.Path.Combine(folder, filename);
                byte[] binData = File.ReadAllBytes(path_en);
                ScriptTokenizer tokenizer = new ScriptTokenizer(new DataWrapper(binData), scriptListFileManager);
                var tokenListTemp = tokenizer.ParseData();
                int index = tokenListTemp.FindIndex(token => token is TokenExtGoto);
                if (index >= 0 && tokenListTemp[index].ByteCommand[2] > removedIndex)
                {
                    tokenListTemp[index].ByteCommand[2]--;
                    SaveFile(filename, tokenizer.AssembleAsData(tokenListTemp));
                }
            }
            
        }
    }


    public class CommandViewBox : INotifyPropertyChanged
    {
        private ObservableCollection<Token> _commandList;
        public ObservableCollection<Token> CommandList
        {
            get => _commandList;
            set
            {
                _commandList = value;
                OnPropertyChanged(nameof(CommandList));
            }
        }

        public CommandViewBox(List<Token> tokenList)
        {
            CommandList = new ObservableCollection<Token>();

            foreach (var token in tokenList)
            {
                CommandList.Add(token);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class ScriptSizeNotifier : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private readonly List<Token> tokenList;

        private int _size;


        public ScriptSizeNotifier(List<Token> tokenList)
        {
            this.tokenList = tokenList;
            tokenList.ForEach(token => token.PropertyChanged += this.Token_PropertyChanged);
            Size = CalculateScriptSize();
        }


        private void OnPropertyChanged(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }

        private void Token_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Size = CalculateScriptSize();
        }

        private int CalculateScriptSize()
        {
            int size = 0;
            tokenList.ForEach(token => size += token.Size);
            return size;
        }

        public int Size
        {
            get
            {
                return _size;
            }
            set
            {
                _size = value;
                OnPropertyChanged("Size");
            }
        }
    }
}