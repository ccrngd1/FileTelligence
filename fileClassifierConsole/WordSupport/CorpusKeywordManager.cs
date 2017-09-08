using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NHunspell;
using WordSupport.Extensions;

namespace WordSupport
{

    public interface IUsableManager
    {
        bool ReadStopList(string file);
        
    }

    public class CorpusKeywordManager : IUsableManager
    {
        public const string compiledDataExtension = "FCC_HunspellWEResult";

        private HashSet<string> _stopList;
        private GlobalWordList _globalKeyword = new GlobalWordList(); //this will hold all of the _keywords and their total count

        private List<DocumentKeywords> _keywords = new List<DocumentKeywords>(); //this will hold _keywords for each file
        private Hunspell hunspell;

        public CorpusKeywordManager()
        {
            _stopList = new HashSet<string>();
        }
        
        public bool ReadStopList(string file)
        {
            StreamReader sr;

            try { sr = new StreamReader(file); }
            catch
            {
                Console.WriteLine("{0} doesn't exist.", file);
                return false;
            }

            string line;

            line = sr.ReadLine();
            while (line != null)
            {
                _stopList.Add(line);
                line = sr.ReadLine();
            }

            sr.Close();
            return true;
        }

        public void Run(string directory, string resourceDirectory, string outputDirectory)
        {
            WordExtraction(directory, resourceDirectory, outputDirectory);

            //ok, so each document name is located in Files[]
            //each documents _keywords are located in keyword[]
            //all of the _keywords are located in globalKeyword for math purposes

            double[,] holder = SimilarityBetweenDocuments();

            //var queryHolder = SimilarityBetweenQueryAndDocuments("windows, azure".Split(',').ToList());

            Console.WriteLine("Here are the results");

            for (var a = 0; a < holder.GetLength(0); a++)
            {
                for (var b = 0; b < holder.GetLength(1); b++)
                {
                    Console.Write("{0:0.000}   ", holder[a, b]);
                }
                Console.Write("\n\r");
            }

            var dist = DistanceBetweenDocuments(_keywords[0].WordList, _keywords[1].WordList);

            var termIFDIF1 = TermTfIdf("account", _keywords[0], _keywords.Select(c => c as DocumentWordList).ToList());
        }

        public void WordExtraction(string directory, string resourceDirectory, string outputDirectory)
        {
            Scanner scanner = null;
            
            ReadStopList(resourceDirectory + "stoplist.txt");
            ReadStopList(resourceDirectory + "CommonWords.txt");

            hunspell = new Hunspell(resourceDirectory + "en_us.aff", resourceDirectory + "en_us.dic");

            //do the counting for every file, to fill the keyword instances
            //will also increase count for the global count
            foreach (var f in new System.IO.DirectoryInfo(directory).EnumerateFiles())
            {
                if (!f.Name.ToLower().EndsWith(".pdf") && !f.Name.ToLower().EndsWith(compiledDataExtension))
                    continue; //only read pdf files and pre-computed data files

                var tempKW = new DocumentKeywords(_stopList, hunspell);
                _keywords.Add(tempKW);

                if (File.Exists(f.Name + "."+ compiledDataExtension))
                {
                    tempKW.DataFile = f.Name;

                    using(var sr = File.OpenText(f.Name + "." + compiledDataExtension))
                    {
                        var jsoned = sr.ReadToEnd();

                        tempKW.WordList = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, WordStats>>(jsoned);
                    }

                    continue;
                }

                IPdfExtraction pClass = new Pdf();

                if (System.IO.File.Exists(f.Name + ".txt."))
                    System.IO.File.Delete(f.Name + ".txt.");

                string extractLocation = f.DirectoryName + "\\" + f.Name + ".txt.";

                pClass.ExtractText(f.FullName, f.DirectoryName+"\\" + f.Name + ".txt.");
                
                try
                {
                    scanner = new Scanner(extractLocation);
                }
                catch
                {
                    Console.WriteLine("File {0} doesn't exist.", extractLocation);
                    continue;
                }

                if (tempKW.InsertWordsFromFile(extractLocation, scanner, Parser._word, Parser._EOF, false))
                {
                    var jsonoutput = Newtonsoft.Json.JsonConvert.SerializeObject(tempKW.WordList, Newtonsoft.Json.Formatting.Indented);

                    using (StreamWriter sw = new StreamWriter(f.FullName + "." + compiledDataExtension))
                    {
                        sw.Write(jsonoutput);
                        sw.Flush();
                    }

                    System.IO.File.Delete(f.Name + ".txt.");
                }

                foreach (KeyValuePair<string, WordStats> kvp in tempKW.WordList)
                {
                    if (_globalKeyword.WordList.ContainsKey(kvp.Key))
                    {
                        _globalKeyword.WordList[kvp.Key].Count += kvp.Value.Count;
                    }
                    else
                    {
                        _globalKeyword.WordList.Add(kvp.Key, new GlobalStats(kvp.Value.Count));
                    }
                }

                Console.WriteLine("Total Keywords for this doc found = {0}", tempKW.WordList.Count);
                Console.WriteLine("Total Keywords  found = {0}", _globalKeyword.WordList.Count);                
            }
        }

        double TermTfIdf(string targetTerm, DocumentWordList targetDoc, List<DocumentWordList> allKeys)
        {
            return TermTfIdf2(targetTerm,
                targetDoc.ToWordListWithGlobalEntries(_globalKeyword),
                allKeys.ToWordListWithGlobalEntries(_globalKeyword));
        }

        double TermTfIdf2(string keyVal, DocumentWordList targetDocWithGlobals, List<DocumentWordList> allKeysWithGlobals)
        {
            //var tf = CalculateTFAdjustedForDocLength(TargetTerm.Key, targetDocWithGlobals);
            //var idf = IDF(keyVal, allKeysWithGlobals);

            var idf = _globalKeyword.WordList[keyVal].IdfValue;

            return targetDocWithGlobals.WordList[keyVal].TFAdjustedForDocLength * idf;
        }

        Dictionary<string, double> TermTfIdf(string TargetTerm, List<DocumentWordList> allKeysWithGlobals)
        {
            var retValue = new Dictionary<string, double>(allKeysWithGlobals.Count);

            foreach (DocumentWordList singleDocKeys in allKeysWithGlobals)
            {
                double result = 0;
                if (singleDocKeys.WordList.ContainsKey(TargetTerm))
                {
                    //var tf = CalculateTFAdjustedForDocLength(TargetTerm, singleDocKeys);
                    //var idf = IDF(TargetTerm, allKeysWithGlobals);

                    var idf = _globalKeyword.WordList[TargetTerm].IdfValue;
                }
                retValue.Add(singleDocKeys.DataFile, result);
            }

            return retValue;
        }

        public Dictionary<string, double> DocumentTFIDF(DocumentWordList targetDic, List<DocumentWordList> allKeys)
        {
            var retVal = new Dictionary<string, double>(targetDic.WordList.Count);
            int i = 0;

            foreach (KeyValuePair<string, WordStats> KVP in targetDic.WordList)
            {
                var IDFval = _globalKeyword.WordList[KVP.Key].IdfValue;

                retVal.Add(KVP.Key, KVP.Value.TFAdjustedForDocLength * IDFval);
                i++;
            }

            return retVal;
        }


        public double[,] SimilarityBetweenDocuments()
        {
            var keywords = _keywords.Select(c => c as DocumentWordList).ToList();

            //for each file with some _keywords
            //we will add in an entry for globalKeywords that were not found in the original file
            //so we will have a lot of [key, 0] entries now
            List<DocumentWordList> fileKeyWordsWithGlobalEntires = keywords.ToWordListWithGlobalEntries(_globalKeyword);

            var tempCalc = new double[keywords.Count, _globalKeyword.WordList.Count];

            for (var i = 0; i < keywords.Count; i++)
            {
                //this gives the TfIdf value for each keyword in a document vs all the documen
                Dictionary<string, double> temp = DocumentTFIDF(fileKeyWordsWithGlobalEntires[i], fileKeyWordsWithGlobalEntires);

                int j = 0;

                foreach (KeyValuePair<string, double> keyValuePair in temp)
                {
                    tempCalc[i, j] = keyValuePair.Value;
                    j++;
                }
            }

            double[,] realRetVal = Utilities.CalcSim(tempCalc, keywords.Count);

            return realRetVal;
        }

        public double[,] SimilarityBetweenQueryAndDocuments(List<string> queryWords)
        {
            var keywords = _keywords.Select(c => c as DocumentWordList).ToList();

            List<DocumentWordList> fileKeyWordsWithGlobalEntires = keywords.ToWordListWithGlobalEntries(_globalKeyword);

            var tempCalc = new double[queryWords.Count, _globalKeyword.WordList.Count];

            for (var i = 0; i < queryWords.Count; i++)
            {
                Dictionary<string, double> temp = TermTfIdf(queryWords[i], fileKeyWordsWithGlobalEntires);

                int j = 0;

                foreach (KeyValuePair<string, double> keyValuePair in temp)
                {
                    tempCalc[i, j] = keyValuePair.Value;
                    j++;
                }
            }

            double[,] realRetVal = Utilities.CalcSim(tempCalc, queryWords.Count);

            return realRetVal;

            return null;
        }

        public double DistanceBetweenDocuments(Dictionary<string, WordStats> d1, Dictionary<string, WordStats> d2)
        {
            double distance = 0;
            // Find the common words between documents
            Dictionary<string, int> commonWords = new Dictionary<string, int>(d1.Count);

            foreach (KeyValuePair<string, WordStats> kvp in d1)
                commonWords[kvp.Key] = 0;

            foreach (KeyValuePair<string, WordStats> kvp in d2)
            {
                if (commonWords.ContainsKey(kvp.Key))
                    commonWords[kvp.Key]++;
            }

            Dictionary<string, int> frequencyListD1 = new Dictionary<string, int>();
            Dictionary<string, int> frequencyListD2 = new Dictionary<string, int>();

            foreach (KeyValuePair<string, int> kvp in commonWords)
            {
                if (kvp.Value > 0)
                {
                    frequencyListD1.Add(kvp.Key, d1[kvp.Key].Count);
                    frequencyListD2.Add(kvp.Key, d2[kvp.Key].Count);
                }
            }

            for (int i = 0; i < frequencyListD1.Count; i++)
            {
                distance += Math.Abs(frequencyListD1.ElementAt(i).Value
                                   - frequencyListD2.ElementAt(i).Value);
            }

            return distance / frequencyListD1.Count;
        }
    }
}