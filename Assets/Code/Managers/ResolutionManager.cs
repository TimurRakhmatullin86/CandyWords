namespace Managers {
    using UnityEngine;

    public class ResolutionManager : MonoBehaviour {
        public static ResolutionManager Instance;

        public float   SizeForCell           => this.sizeForCell;
        public float   SizeForText           => this.sizeForText;
        public float   SizeForTitleText      => this.sizeForTitleText;
        public float   SizeForSquareUiImages => this.sizeForSquareUiImages;
        public Vector2 SizeForButtons        => this.sizeForButtons;

        private float   sizeForCell;
        private float   sizeForText;
        private float   sizeForTitleText;
        private float   sizeForSquareUiImages;
        private Vector2 sizeForButtons;

        public void ArrangeUiSizesAccordingToTheScreenWidth() {
            this.sizeForCell = Screen.width / 10.5f;

            if (Screen.width <= 720) {
                this.sizeForText           = 35;
                this.sizeForTitleText      = 70;
                this.sizeForButtons        = new Vector2(160, 138);
                this.sizeForSquareUiImages = 75f;
            }
            else if (Screen.width >= 1440) {
                this.sizeForText      = 60;
                this.sizeForTitleText = 120;
                this.sizeForButtons        = new Vector2(300, 260);
                this.sizeForSquareUiImages = 110f;
            }
            else {
                this.sizeForText           = 50;
                this.sizeForTitleText      = 100;
                this.sizeForButtons        = new Vector2(256, 220);
                this.sizeForSquareUiImages = 91f;
            }
        }

        private void Awake() => Instance = this;
    }
}