// using System.Collections.Generic;
// using Structures;

// [System.Serializable]
// public class PlayerProgress {
//     public Dictionary<CandyColor, HashSet<string>> learnedWords = new Dictionary<CandyColor, HashSet<string>>();

//     public void AddLearnedWord(CandyColor color, string word) {
//         if (!learnedWords.ContainsKey(color)) {
//             learnedWords[color] = new HashSet<string>();
//         }
//         learnedWords[color].Add(word);
//     }

//     public int GetLearnedWordsCount(CandyColor color) {
//         return learnedWords.ContainsKey(color) ? learnedWords[color].Count : 0;
//     }
// }