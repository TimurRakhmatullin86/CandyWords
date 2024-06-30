namespace Structures {
    using System.Collections.Generic;

    public struct MatchedCase {
        public readonly List<Cell>  Candidates;
        public readonly Cell        TargetCell;
        public readonly MatchedType MatchedType;
        public readonly CandyColor  CandyColor;

        public MatchedCase(List<Cell> candidates, Cell targetCell, MatchedType matchedType, CandyColor candyColor) {
            this.Candidates  = candidates;
            this.TargetCell  = targetCell;
            this.MatchedType = matchedType;
            this.CandyColor  = candyColor;
        }
    }
}