using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordSupport
{
    public enum WordUsage
    {
        Other = 0,
        Noun = 1,
        Verb = 2,
    }

    public class WordCounts
    {
        public string Word;
        public int Count;
        public WordRelations WordRelations;
    }

    public class WordRelations
    {
        public string WordCategorizer;
        public System.Collections.Hashtable RelatedWords = new System.Collections.Hashtable();

        public WordRelations(string mainWord, List<WordRelations> relatedWords)
        {
            WordCategorizer = mainWord;

            if (relatedWords == null) return;

            foreach(var s in relatedWords)
            {
                RelatedWords.Add(s.WordCategorizer.ToLower(), s );
            }
        }
    }

    public class GarbageWord
    {
        public WordUsage Use { get; set; }
        public string Word { get; set; }

        public GarbageWord() { }
        public GarbageWord(WordUsage use, string w)
        {
            Use = use;
            Word = w;
        }
    }

    public class GarbageWordCollection
    {
        public Dictionary<string, WordUsage> CommonWords = new Dictionary<string, WordUsage>();

    }
}
