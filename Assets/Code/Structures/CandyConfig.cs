namespace Structures {
    using System;

    [Serializable]
    public struct CandyConfig {
        public CandyColor CandyColor;
        public CandyType  CandyType;

        public CandyConfig(CandyColor candyColor, CandyType candyType) {
            this.CandyColor = candyColor;
            this.CandyType  = candyType;
        }
    }
}