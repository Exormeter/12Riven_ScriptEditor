/*
 * Issues:
 * Automatically create archive
 *  Randomly spits out gibberish then dies
 *  Text on choices is misaligned severely
 *  I think the way I made TokenIf create an IntGoto token is messing things up
 *      Nope, something else is causing it. Look for commands that happen before it screws up.
 */


using System.Collections.Generic;

namespace Riven_Script_Editor.Tokens
{
    public abstract class ATokenizer
    {
        protected DataWrapper data;
        public ATokenizer(DataWrapper wrapper)
        {
            data = wrapper;
        }
        bool ChangedFile
        {
            get;
            set;
        }
        public abstract List<Token> ParseData();


        public abstract byte[] AssembleAsData(List<Token> tokens);

        public abstract byte[] AssembleAsText(string title, List<Token> tokens);
    }
}