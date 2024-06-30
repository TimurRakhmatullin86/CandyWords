using UnityEngine;
using System.Collections.Generic;
using Structures;

// namespace WordServices
// {
    [System.Serializable]
    public class WordCategory
    {
        public CandyColor color;
        public List<string> words;
        public List<string> translations;
    }

    [CreateAssetMenu(fileName = "WordDatabase", menuName = "ScriptableObjects/WordDatabase")]
    public class WordDatabase : ScriptableObject
    {
        public List<WordCategory> categories;

        public (string word, string translation) GetRandomWord(CandyColor color)
        {
            var category = categories.Find(c => c.color == color);
            if (category != null && category.words.Count > 0 && category.words.Count == category.translations.Count)
            {
                int randomIndex = Random.Range(0, category.words.Count);
                return (category.words[randomIndex], category.translations[randomIndex]);
            }
            return ("No word found", "Слово не найдено");
        }
    }
// } 