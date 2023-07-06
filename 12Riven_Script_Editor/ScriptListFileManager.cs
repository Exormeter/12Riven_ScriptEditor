using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Riven_Script_Editor
{
    public class ScriptListFileManager
    {
        private ObservableCollection<string> _displayedScriptNameFiles = new ObservableCollection<string>();
        private List<string> _completeFilenameList = new List<string>();
        private string listFilePath;

        public ObservableCollection<string> ScriptFilenameList
        {
            get { return _displayedScriptNameFiles; }
        }

        public List<string> CompleteFilenameList
        {
            get { return _completeFilenameList;  }
        }
        public ScriptListFileManager(string listFilePath)
        {
            Load(listFilePath);
        }

        public int AddFilename(string filename)
        {
            _displayedScriptNameFiles.Add(filename);
            _completeFilenameList.Add(filename);
            SaveFilenameList();
            return _completeFilenameList.Count() - 1;
        }

        public int RemoveFilename(string filename)
        {
            int index = _completeFilenameList.IndexOf(filename);
            _displayedScriptNameFiles.Remove(filename);
            _completeFilenameList.Remove(filename);
            File.Delete(listFilePath + filename);
            SaveFilenameList();
            return index;
        }

        public int getFilenameIndex(string filename)
        {
            return _completeFilenameList.IndexOf(filename);
        }

        public void SaveFilenameList()
        {
            File.WriteAllLines(listFilePath, _completeFilenameList); 
        }

        public void Load(string listFilePath)
        {
            this.listFilePath = listFilePath;
            _displayedScriptNameFiles.Clear();
            Regex sceneNamePattern = new Regex("^([A-z]{2}[0-9]{2}).*");
            if (File.Exists(listFilePath))
            {
                foreach (string filename in File.ReadAllLines(listFilePath).ToList())
                {
                    if (filename.Equals("DATA.BIN") || filename.Equals("Repi.BIN") || sceneNamePattern.IsMatch(filename))
                    {
                        _displayedScriptNameFiles.Add(filename);
                    }
                    _completeFilenameList.Add(filename);
                }
            }
        }
    }
}
