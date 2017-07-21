using System.Collections.Generic; 

namespace WordSupport.Extensions
{
    public static class KeywordsExt
    {
        /// <summary>
        ///for each file with some keywords
        ///we will add in an entry for globalKeywords that were not found in the original file
        ///so we will have a lot of [key, 0] entries now
        /// </summary>
        /// <param name="keywords"></param>
        /// <param name="globalKeywords"></param>
        /// <returns></returns>
        public static List<DocumentWordList> ToWordListWithGlobalEntries(this List<DocumentWordList> keywords,
            GlobalWordList globalKeywords)
        {
            var fileKeyWordsWithGlobalEntires = new List<DocumentWordList>(keywords.Count);
            
            for (var j = 0; j < keywords.Count; j++)
            {
                fileKeyWordsWithGlobalEntires.Add(new DocumentWordList());

                foreach (var kvp in globalKeywords.WordList)
                {
                    fileKeyWordsWithGlobalEntires[j].WordList.Add(kvp.Key, new WordStats(0));

                    if (keywords[j].WordList.ContainsKey(kvp.Key))
                    {
                        fileKeyWordsWithGlobalEntires[j].WordList[kvp.Key] = keywords[j].WordList[kvp.Key];
                    }
                }

                //fileKeyWordsWithGlobalEntires[j].SortList(SortMethod.KeyAsc);
            }

            return fileKeyWordsWithGlobalEntires;
        }

        public static DocumentWordList ToWordListWithGlobalEntries(this DocumentWordList keywords,
            GlobalWordList globalKeywords)
        {
            var fileKeyWordsWithGlobalEntires = new DocumentWordList();

            foreach (var kvp in globalKeywords.WordList)
            {
                fileKeyWordsWithGlobalEntires.WordList.Add(kvp.Key, new WordStats(0));

                if (keywords.WordList.ContainsKey(kvp.Key))
                {
                    fileKeyWordsWithGlobalEntires.WordList[kvp.Key] = keywords.WordList[kvp.Key];
                }
            }

            //fileKeyWordsWithGlobalEntires.SortList(SortMethod.KeyAsc);

            return fileKeyWordsWithGlobalEntires;
        }
    }
}
