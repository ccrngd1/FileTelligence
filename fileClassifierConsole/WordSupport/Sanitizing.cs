using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WordSupport
{
    public class Sanitizing
    {
        public GarbageWordCollection gwc = new GarbageWordCollection();

        public string SanitizeWord(string s)
        {
            Regex rgx = new Regex("[^a-zA-Z]");
            var singleWord =rgx.Replace(s.ToLower(), "");

            return singleWord;
        }

        public void PopulateCommonWords(string commonWordsFile)
        {
            using (TextReader tr = new StreamReader(commonWordsFile))
            {
                string line = tr.ReadLine()?.ToLower();

                while (line != null)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        if(!gwc.CommonWords.ContainsKey(line))
                            gwc.CommonWords.Add(line, WordUsage.Other);
                    }

                    line = tr.ReadLine()?.ToLower();
                }
            }
        }
    }
}
