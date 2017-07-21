using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WordSupport
{
    public class WordExtraction
    {
        public Dictionary<string, Dictionary<string, WordCounts>> foundWordCounts = new Dictionary<string, Dictionary<string, WordCounts>>();
        Sanitizing sant = new Sanitizing();

        public WordExtraction()
        {
           
        }

        public void SetUpSantization(string commonWordFile)
        {
            sant.PopulateCommonWords(commonWordFile);
        }

        public bool ExtractWords(string file)
        {
            Dictionary<string, WordCounts> WordCountsForFile = new Dictionary<string, WordCounts>();

            using (System.IO.TextReader tr = new System.IO.StreamReader(file))
            {
                string line = tr.ReadLine().ToLower();
                string holdOverWord = "";
                bool brokenWord = false;

                while (line!=null)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        if (line.Last() == '-') brokenWord = true;

                        var splitLine = line.Split(new[] { " ","-" }, StringSplitOptions.RemoveEmptyEntries);

                        if (splitLine == null || !splitLine.Any()) {
                            line = tr.ReadLine().ToLower();
                            continue;
                        };

                        for(int i =0; i<splitLine.Length;i++)
                        {
                            if(i==0 && !string.IsNullOrWhiteSpace(holdOverWord))
                            {
                                splitLine[0] = holdOverWord.Replace("-","") + splitLine[0];
                                holdOverWord = "";
                            }

                            //hold this partial word over because it broke mid word
                            if (i == splitLine.Length - 1 && brokenWord)
                            {
                                holdOverWord = splitLine[i];
                                line = tr.ReadLine().ToLower();
                                brokenWord = false;
                                continue;
                            }
                                                        
                            string singleWord = sant.SanitizeWord(splitLine[i]);

                            WordCounts tempWC = null;
                            if (WordCountsForFile.ContainsKey(singleWord))
                            {
                                tempWC = WordCountsForFile[singleWord];
                            }
                            else if (WordCountsForFile.ContainsKey(singleWord.Replace("-", "")))
                            {
                                tempWC = WordCountsForFile[singleWord.Replace("-", "")];
                            }

                            if (tempWC == null)
                            {
                                tempWC = new WordCounts()
                                {
                                    Count = 0,
                                    Word = singleWord,
                                    WordRelations = new WordRelations(singleWord, null)
                                };
                                WordCountsForFile.Add(singleWord, tempWC);
                            }

                            tempWC.Count++;
                        }
                    }

                    line = tr.ReadLine()?.ToLower();
                }
            }

            foundWordCounts.Add(file, WordCountsForFile);

            return true;
        }

        public void RemoveCommonGarbageWords()
        {
            var keyList = foundWordCounts.Keys.ToList();

            for (int i=0; i < keyList.Count(); i++)
            {
                var kvp = keyList[i];

                if (sant.gwc.CommonWords.ContainsKey(kvp))
                {
                    foundWordCounts.Remove(kvp);
                }
            }
        }
    }
}
