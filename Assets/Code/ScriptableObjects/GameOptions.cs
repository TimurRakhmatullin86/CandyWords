namespace ScriptableObjects {
    using Structures;
    using UnityEngine;

    [CreateAssetMenu(fileName = "GameOptions")]
    public class GameOptions : ScriptableObject {
        public CandyColor DesiredColorToHave   = CandyColor.Blue;
        public bool       IsDesiredColorRandom = true;
        public int        Swipes               = 20;
        public int        DesiredAmount        = 40;

        public float SuggestTimer = 1.8f;
        public float SwapingTime  = .3f;
        public float FallingTime  = .7f;

        public string LevelTheme = "Artificial Intelligence";
    public CandyColor[] ThemeColors = new CandyColor[5]; // Цвета для каждой подтемы
    }
}