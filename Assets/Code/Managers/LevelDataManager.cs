namespace Managers {
    using Structures;
    using UniRx;
    using UnityEngine;

    public class LevelDataManager : MonoBehaviour {
        public static LevelDataManager Instance;

        public ReactiveProperty<int> BlueCandiesCount   = new ReactiveProperty<int>();
        public ReactiveProperty<int> GreenCandiesCount  = new ReactiveProperty<int>();
        public ReactiveProperty<int> OrangeCandiesCount = new ReactiveProperty<int>();
        public ReactiveProperty<int> PurpleCandiesCount = new ReactiveProperty<int>();
        public ReactiveProperty<int> RedCandiesCount    = new ReactiveProperty<int>();
        public ReactiveProperty<int> Gold               = new ReactiveProperty<int>();

        private int multiplierFromCandy = 10;

        public void EmptyTheScores() {
            this.BlueCandiesCount.Value   = 0;
            this.GreenCandiesCount.Value  = 0;
            this.OrangeCandiesCount.Value = 0;
            this.PurpleCandiesCount.Value = 0;
            this.RedCandiesCount.Value    = 0;
            this.Gold.Value               = 0;
        }

        public void CandiesIncrease((CandyColor color, CandyType type) tuple) {
            var goldMultiplier = 1;

            switch (tuple.type) {
                case CandyType.Bomb:
                    goldMultiplier = 3;

                    break;

                case CandyType.StripedVert:
                    goldMultiplier = 3;

                    break;

                case CandyType.StripedHor:
                    goldMultiplier = 2;

                    break;
            }

            switch (tuple.color) {
                case CandyColor.Blue:
                    this.BlueCandiesCount.Value++;

                    break;

                case CandyColor.Orange:
                    this.OrangeCandiesCount.Value++;

                    break;

                case CandyColor.Green:
                    this.GreenCandiesCount.Value++;

                    break;

                case CandyColor.Purple:
                    this.PurpleCandiesCount.Value++;

                    break;

                case CandyColor.Red:
                    this.RedCandiesCount.Value++;

                    break;
            }

            this.Gold.Value += goldMultiplier * this.multiplierFromCandy;
        }

        private void Awake() => Instance = this;
    }
}