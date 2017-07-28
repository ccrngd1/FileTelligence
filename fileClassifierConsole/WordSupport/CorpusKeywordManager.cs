using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CSML;
using NHunspell;
using WordSupport.Extensions;

namespace WordSupport
{
    public class CorpusKeywordManager
    {
        private HashSet<string> _stopList;
        private GlobalWordList _globalKeyword = new GlobalWordList(); //this will hold all of the keywords and their total count

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

        public GlobalWordList Run(string directory, string resourceDirectory, string outputDirectory)
        {
            List<DocumentKeywords> keywords; //this will hold keywords for each file

            Scanner scanner = null;

            var di = new DirectoryInfo(directory);

            FileInfo[] fi = di.GetFiles("*.txt");

            keywords = new List<DocumentKeywords>(fi.Length);

            ReadStopList(resourceDirectory + "stoplist.txt");
            ReadStopList(resourceDirectory + "CommonWords.txt");

            hunspell = new Hunspell(resourceDirectory + "en_us.aff", resourceDirectory + "en_us.dic");

            //do the counting for every file, to fill the keyword instances
            //will also increase count for the global count
            for (int i = 0; i < fi.Length; i++)
            {
                keywords.Add(new DocumentKeywords(_stopList, hunspell));

                try
                {
                    scanner = new Scanner(fi[i].FullName);
                }
                catch
                {
                    Console.WriteLine("File {0} doesn't exist.", fi[i].FullName);
                    continue;
                }

                if (keywords[i].InsertWordsFromFile(fi[i].FullName, scanner, Parser._word, Parser._EOF, false))
                {
                    //keywords[i].SortList(SortMethod.KeyAsc);
                    //Console.WriteLine("There are {0} words in {1}", keyword[i].WordList.Count, fi[i].FullName);
                    //Console.WriteLine("Write word counts to " + fi[i].Name + ".wc");
                    keywords[i].WriteWordList(outputDirectory + "\\Results\\", fi[i].Name + ".wc");
                }

                foreach (KeyValuePair<string, WordStats> kvp in keywords[i].WordList)
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
                Console.WriteLine("Total Keywords for this doc found = {0}", keywords[i].WordList.Count);
                Console.WriteLine("Total Keywords  found = {0}", _globalKeyword.WordList.Count);
            }

            //ok, so each document name is located in Files[]
            //each documents keywords are located in keyword[]
            //all of the keywords are located in globalKeyword for math purposes

            double[,] holder = SimilarityBetweenDocuments(keywords.Select(c => c as DocumentWordList).ToList(), _globalKeyword);

            var queryHolder = SimilarityBetweenQueryAndDocuments("windows, azure".Split(',').ToList(), keywords.Select(c => c as DocumentWordList).ToList(), _globalKeyword);

            Console.WriteLine("Here are the results");

            for (var a = 0; a < holder.GetLength(0); a++)
            {
                for (var b = 0; b < holder.GetLength(1); b++)
                {
                    Console.Write("{0:0.000}   ", holder[a, b]);
                }
                Console.Write("\n\r");
            }

            var dist = DistanceBetweenDocuments(keywords[0].WordList, keywords[1].WordList);

            var termIFDIF1 = TermTfIdf("account", keywords[0], keywords.Select(c => c as DocumentWordList).ToList(), _globalKeyword);

            return _globalKeyword;
        }
























        double TermTfIdf(string targetTerm, DocumentWordList targetDoc, List<DocumentWordList> allKeys, GlobalWordList globalKeywords)
        {
            return TermTfIdf(targetTerm,
                targetDoc.ToWordListWithGlobalEntries(globalKeywords),
                allKeys.ToWordListWithGlobalEntries(globalKeywords));
        }

        double TermTfIdf(string keyVal, DocumentWordList targetDocWithGlobals, List<DocumentWordList> allKeysWithGlobals)
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

        Dictionary<string, double> DocumentTFIDF(DocumentWordList targetDic, List<DocumentWordList> allKeys)
        {
            var retVal = new Dictionary<string, double>(targetDic.WordList.Count);
            int i = 0;

            foreach (KeyValuePair<string, WordStats> KVP in targetDic.WordList)
            {
                //double TFval = CalculateTFAdjustedForDocLength(KVP.Key, allKeys[targetDoc]);
                //double IDFval = IDF(KVP.Key, allKeys);

                var IDFval = _globalKeyword.WordList[KVP.Key].IdfValue;

                retVal.Add(KVP.Key, KVP.Value.TFAdjustedForDocLength * IDFval);
                i++;
            }

            return retVal;
        }

        double[,] CalcSim(double[,] MatrixTFIDF, int N)
        {
            double[,] retVal = new double[N, N];
            Matrix A;
            Matrix B;

            for (int i = 0; i < N; i++)
            {
                for (int j = i; j < N; j++)
                {
                    if (j == i)
                    { retVal[i, j] = 1; continue; }

                    string temp = "";
                    string temp2 = "";
                    for (int k = 0; k < MatrixTFIDF.GetLength(1); k++)
                    {
                        temp += MatrixTFIDF[i, k].ToString() + ",";
                        temp2 += MatrixTFIDF[j, k].ToString() + ",";
                    }
                    temp = temp.Remove(temp.Length - 1);
                    temp2 = temp2.Remove(temp2.Length - 1);
                    A = new Matrix(temp);
                    B = new Matrix(temp2);

                    retVal[i, j] = Sim(A, B);
                }
            }

            return retVal;
        }

        /// <summary>
        /// A.B / ||A|| ||B||
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        double Sim(Matrix a, Matrix b)
        {
            double retVal = 0;

            double top = Matrix.Dot(a, b);

            double Adot = Math.Sqrt(Matrix.Dot(a, a));
            double Bdot = Math.Sqrt(Matrix.Dot(b, b));

            double bot = Math.Sqrt(Matrix.Dot(a, a)) * Math.Sqrt(Matrix.Dot(b, b));

            return retVal = top / bot;
        }

        double[,] SimilarityBetweenDocuments(List<DocumentWordList> keywords, GlobalWordList globalKeywords)
        {
            //for each file with some keywords
            //we will add in an entry for globalKeywords that were not found in the original file
            //so we will have a lot of [key, 0] entries now
            List<DocumentWordList> fileKeyWordsWithGlobalEntires = keywords.ToWordListWithGlobalEntries(globalKeywords);

            var tempCalc = new double[keywords.Count, globalKeywords.WordList.Count];

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

            double[,] realRetVal = CalcSim(tempCalc, keywords.Count);

            return realRetVal;
        }

        public double[,] SimilarityBetweenQueryAndDocuments(List<string> queryWords, List<DocumentWordList> keywords, GlobalWordList globalKeywords)
        {
            //List<DocumentWordList> fileKeyWordsWithGlobalEntires = keywords.ToWordListWithGlobalEntries(globalKeywords);

            //var tempCalc = new double[queryWords.Count, globalKeywords.WordList.Count];

            //for (var i = 0; i < queryWords.Count; i++)
            //{
            //    Dictionary<string, double> temp = DocumentTFIDF(queryWords[i], fileKeyWordsWithGlobalEntires);

            //    int j = 0;

            //    foreach (KeyValuePair<string, double> keyValuePair in temp)
            //    {
            //        tempCalc[i, j] = keyValuePair.Value;
            //        j++;
            //    }
            //}

            //double[,] realRetVal = CalcSim(tempCalc, queryWords.Count);

            //return realRetVal;

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