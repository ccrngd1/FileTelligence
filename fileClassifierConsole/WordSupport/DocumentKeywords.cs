using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using NHunspell;

namespace WordSupport
{
    public enum SortMethod
    {
        KeyAsc,
        KeyDesc,
        ValueAsc,
        ValueDesc
    }

    public class GlobalStats : WordStats
    {
        public double IdfValue { get; set; }

        public GlobalStats(int count) : base(count)
        {

        }
    }

    public class WordStats
    {
        public WordStats(int count)
        {
            Count = count;
        }
        public int Count { get; set; }
        public double TFAdjustedForDocLength { get; set; }
    }

    public class GlobalWordList
    {
        public Dictionary<string, GlobalStats> WordList { get; private set; }

        public GlobalWordList()
        {
            WordList = new Dictionary<string, GlobalStats>();
        }


        /// <summary>
        /// total docs analysed
        /// divided by number of docs that have term ti
        /// </summary>
        public void CalculateGlobalIDF(List<DocumentWordList> entireDocCorpus)
        {

            foreach (KeyValuePair<string, GlobalStats> k in WordList) //check each document to see if the word is contained
            {
                int sum = 0;

                sum = entireDocCorpus.Count(c => c.WordList.ContainsKey(k.Key));

                k.Value.IdfValue = Math.Log10((double)entireDocCorpus.Count / (double)sum);
            }
        }

        ///// <summary>
        ///// total docs analysed
        ///// divided by number of docs that have term ti
        ///// </summary>
        ///// <param name="KeyVal"></param>
        ///// <param name="allKeysWithGlobals"></param>
        ///// <returns></returns>
        double IDF(string KeyVal, List<DocumentWordList> allKeysWithGlobals)
        {
            double retVal = 0;
            int sum = 0;

            foreach (var k in allKeysWithGlobals) //check each document to see if the word is contained
            {
                if (k.WordList[KeyVal].Count > 0)
                    sum++;
            }

            retVal = Math.Log10((double)allKeysWithGlobals.Count / (double)sum);

            return retVal;
        }
    }

    public class DocumentWordList
    {
        public DocumentWordList()
        {
            WordList = new Dictionary<string, WordStats>();
        }

        public string DataFile { get; set; }
        public Dictionary<string, WordStats> WordList { get; private set; }

        private List<string> _wordsOrderedByCount = new List<string>();

        public List<string> WordsByCount
        {
            get
            {
                if (_wordsOrderedByCount == null || !_wordsOrderedByCount.Any())
                {
                    _wordsOrderedByCount = WordList.OrderByDescending(c => c.Value.Count).Select(d => d.Key).ToList();
                }

                return _wordsOrderedByCount;
            }
        }

        #region Sort Word List Method


        //public void SortList(SortMethod sm)
        //{
        //    IEnumerable<KeyValuePair<string, WordStats>> items = (from entry in WordList select entry);

        //    switch (sm)
        //    {
        //        case SortMethod.KeyAsc:
        //            items = (from entry in WordList orderby entry.Key ascending select entry);
        //            break;
        //        case SortMethod.KeyDesc:
        //            items = (from entry in WordList orderby entry.Key descending select entry);
        //            break;
        //        case SortMethod.ValueAsc:
        //            items = (from entry in WordList orderby entry.Value ascending select entry);
        //            break;
        //        case SortMethod.ValueDesc:
        //            items = (from entry in WordList orderby entry.Value descending select entry);
        //            break;
        //    }
        //    Dictionary<string, WordStats> result = items.ToDictionary(i => i.Key, i => i.Value);

        //    WordList.Clear();
        //    WordList = result;
        //}

        #endregion Sort Word List Method

        #region WordList Print and Write methods

        public void PrintWordList()
        {
            foreach (KeyValuePair<string, WordStats> entry in WordList)
                Console.WriteLine("{0} \t\t\t {1}", entry.Key, entry.Value);
        }

        public bool WriteWordList(string directory, string fileName)
        {
            if (WordList.Count == 0) return false;

            if (!System.IO.Directory.Exists(directory))
                System.IO.Directory.CreateDirectory(directory);

            string file = directory + "\\" + fileName;

            if (System.IO.File.Exists(file))
                System.IO.File.Delete(file);

            using (StreamWriter sw = System.IO.File.CreateText(file))
            {
                foreach (KeyValuePair<string, WordStats> entry in WordList)
                    sw.Write(entry.Key + " \t\t,\t\t " + entry.Value + Environment.NewLine);
            }

            return true;
        }
        #endregion WordList Print and Write methods

    }

    public class DocumentKeywords : DocumentWordList, IDisposable
    {
        #region Fields and Constructors 
        private readonly HashSet<string> _stopList;
        private Hunspell _hunspell;

        public DocumentKeywords(HashSet<string> stopsList, Hunspell hunspellInst)
        {
            _stopList = stopsList;
            _hunspell = hunspellInst;
        }

        #endregion Fields and Constructors


        #region Insert words into wordList methods

        public bool InsertWord(string word)
        {
            List<string> stems = _hunspell.Stem(word);

            if (stems.Count > 0)
            {
                if (!_stopList.Contains(stems[stems.Count - 1]))
                {
                    if (WordList.ContainsKey(stems[stems.Count - 1]))
                        WordList[stems[stems.Count - 1]].Count++;
                    else
                        WordList.Add(stems[stems.Count - 1], new WordStats(1));
                }
                return true;
            }

            return false;
        }

        public bool InsertWordsFromFile(string file, Scanner scanner, int _word, int _EOF, bool disposeHunspell)
        {
            if (scanner == null) return false;
            DataFile = file;

            Token t = scanner.Scan(); // get first token

            while (t.kind != _EOF)
            {
                if (t.kind == _word) // word
                { 
                    //this will insert the base word
                    //IE "will" instead of "willing"
                    //IE "be" instead of "being"
                    InsertWord(t.val.ToLower());

                }
                t = scanner.Scan();
            }

            foreach (KeyValuePair<string, WordStats> keyValuePair in WordList)
            {
                keyValuePair.Value.TFAdjustedForDocLength =
                CalculateTFAdjustedForDocLength(keyValuePair.Key);
            }

            return true;
        }

        /// <summary>
        /// n[ij]/SUM[k](n[kj])
        /// number of occurrances of condisered term (ti) in document (dj)
        /// divided by sum of number of occurrances of all terms in document (dj)
        /// </summary>
        /// <param name="targetTerm"></param>
        /// <param name="targetDoc"></param>
        /// <returns></returns>
        public double CalculateTFAdjustedForDocLength(string targetTerm)
        {
            return WordSupport.Utilities.CalculateTFAdjustedForDocLength(WordList, targetTerm);
        }

        /**
        * Attempt to fix the stemming problem.
        * As noted in class, this code picks the first entry in the list of 
        * stems returned from the Hunspell Stem() method. For the word shaking,
        * the method returns [shaking, shake] where shake returns only [shake].
        * Shaking and shake should have the same step.
        * 
        * This method loops through the array of stems returned by the Stem() 
        * method and finds the shortest stem.
        */
        protected string shortestStem(List<string> stems)
        {
            string stem = null;
            if (stems.Count > 0)
            {
                stem = stems[0];
                if (stems.Count > 1)
                {
                    for (int i = 1; i < stems.Count; i++)
                    {
                        if (stems[i].Length < stem.Length)
                        {
                            stem = stems[i];
                        }
                    }
                }
            }

            return stem;
        }

        #endregion Insert words into wordList methods



        public void Dispose()
        {
            if (_hunspell != null) _hunspell = null;
        }
    }
}