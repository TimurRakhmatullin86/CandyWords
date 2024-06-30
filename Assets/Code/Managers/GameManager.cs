namespace Managers {
    using System.Collections;
    using UI;
    using UniRx;
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Structures;
    using Controllers;
    using ScriptableObjects;
    using UnityEngine;
    using TMPro;

    public class GameManager : MonoBehaviour {
        [SerializeField] private GameOptions    gameOptions;
        [SerializeField] private CandiesOptions candiesOptions;
        
        [SerializeField] private WordDatabase wordDatabase;
        [SerializeField] private TextMeshProUGUI wordDisplayText;
        [SerializeField] private TextMeshProUGUI translationDisplayText;
        // UI Components
        [SerializeField] private LoosePanel   loosePanel;
        [SerializeField] private WinPanel     winPanel;
        [SerializeField] private ScoresPanel  scoresPanel;
        [SerializeField] private ControlPanel controlPanel;
        

        // Singleton Managers/ Controllers
        private ResolutionManager resolutionManager;
        private MatrixController  matrixController;
        private PoolManager       poolManager;
        private LevelDataManager  levelDataManager;
        private AudioManager      audioManager;

        private ReactiveProperty<int> SwipesRemainsCount    = new ReactiveProperty<int>();
        private ReactiveProperty<int> ObservableColourCount = new ReactiveProperty<int>();

        private bool isOnPause;
        private bool isGameOver;

        private async void Awake() {

            this.resolutionManager = ResolutionManager.Instance;
            this.poolManager       = PoolManager.Instance;
            this.matrixController  = MatrixController.Instance;
            this.levelDataManager  = LevelDataManager.Instance;
            this.audioManager      = AudioManager.Instance;

            while (this.resolutionManager == null) {
                await Task.Delay(TimeSpan.FromSeconds(.1f));
                this.resolutionManager = ResolutionManager.Instance;
            }

            while (this.levelDataManager == null) {
                await Task.Delay(TimeSpan.FromSeconds(.1f));
                this.levelDataManager = LevelDataManager.Instance;
            }

            while (this.matrixController == null) {
                await Task.Delay(TimeSpan.FromSeconds(.1f));
                this.matrixController = MatrixController.Instance;
            }

            while (this.poolManager == null) {
                await Task.Delay(TimeSpan.FromSeconds(.1f));
                this.poolManager = PoolManager.Instance;
            }

            while (this.audioManager == null) {
                await Task.Delay(TimeSpan.FromSeconds(.1f));
                this.audioManager = AudioManager.Instance;
            }


            this.SwipesRemainsCount.Value = this.gameOptions.Swipes;


            this.matrixController.GameOptions       =  this.gameOptions;
            this.matrixController.OnCandyDestroyed  += this.OnCandyDestroyed;
            this.matrixController.OnSuccesfullSwipe += () => this.SwipesRemainsCount.Value--;
            this.matrixController.OnBombCreated     += this.audioManager.OnBombCreated;
            this.matrixController.OnSwiped          += this.audioManager.PlaySwipe;

            this.winPanel.RestartButton.onClick.AddListener(this.RestartTheGame);
            this.loosePanel.RestartButton.onClick.AddListener(this.RestartTheGame);

            this.SwipesRemainsCount
                .Where(value => value == 0)
                .Where(_ => !this.isGameOver)
                .Subscribe(_ => {
                     if (this.ObservableColourCount.Value < this.gameOptions.DesiredAmount) {
                         this.StartCoroutine(this.LooseTheGame());
                     }
                     else {
                         this.StartCoroutine(this.WinTheGame());
                     }
                 });

            this.controlPanel.GameControl.onClick.AddListener(() => {
                this.isOnPause = !this.isOnPause;

                if (this.isOnPause) {
                    Time.timeScale                         = 0;
                    this.controlPanel.GameControlText.text = "Play";
                }
                else {
                    Time.timeScale                         = 1;
                    this.controlPanel.GameControlText.text = "Pause";
                }

                this.matrixController.IsOnPause = this.isOnPause;
            });

            // Candies Counting
            this.levelDataManager.BlueCandiesCount
                .Where(value => value > 0)
                .Subscribe(value => this.scoresPanel.BlueScores.text = value.ToString());

            this.levelDataManager.GreenCandiesCount
                .Where(value => value > 0)
                .Subscribe(value => this.scoresPanel.GreenScores.text = value.ToString());

            this.levelDataManager.OrangeCandiesCount
                .Where(value => value > 0)
                .Subscribe(value => this.scoresPanel.OrangeScores.text = value.ToString());

            this.levelDataManager.PurpleCandiesCount
                .Where(value => value > 0)
                .Subscribe(value => this.scoresPanel.PurpleScores.text = value.ToString());

            this.levelDataManager.RedCandiesCount
                .Where(value => value > 0)
                .Subscribe(value => this.scoresPanel.RedScores.text = value.ToString());

            this.levelDataManager.Gold
                .Where(value => value > 0)
                .Subscribe(value => this.scoresPanel.GoldScores.text = value.ToString());

            this.scoresPanel.SwipesRemainsCount.text = this.gameOptions.Swipes.ToString();

            this.SwipesRemainsCount
                .Subscribe(value => this.scoresPanel.SwipesRemainsCount.text = value.ToString());

            this.SplitCandiesConfigsFromOptionsToMatrixControllerByCandyType();
            this.EstablishObservationForDesirableCandyCollected();


            this.resolutionManager.ArrangeUiSizesAccordingToTheScreenWidth();

            this.poolManager.Initialize(this.candiesOptions.Pools);
            this.matrixController.Initialize(this.resolutionManager.SizeForCell);

            this.InitUI();
            this.matrixController.OnCandyMatched += this.OnCandyMatched;
        }

        private void OnCandyMatched(CandyColor color) {
        // Здесь можно добавить логику для обновления UI с прогрессом игрока
    }

        private void InitUI() {
            string goalText =
                $"Collect {this.gameOptions.DesiredAmount.ToString()} {this.gameOptions.DesiredColorToHave.ToString().ToLower()} candies in {this.gameOptions.Swipes.ToString()} swipes";

            this.scoresPanel.BlueScores.fontSize   = this.resolutionManager.SizeForText;
            this.scoresPanel.GreenScores.fontSize  = this.resolutionManager.SizeForText;
            this.scoresPanel.OrangeScores.fontSize = this.resolutionManager.SizeForText;
            this.scoresPanel.PurpleScores.fontSize = this.resolutionManager.SizeForText;
            this.scoresPanel.RedScores.fontSize    = this.resolutionManager.SizeForText;
            this.scoresPanel.GoldScores.fontSize   = this.resolutionManager.SizeForText;

            var squareSize = this.resolutionManager.SizeForSquareUiImages;

            this.scoresPanel.BlueCandiesImage.GetComponent<RectTransform>().sizeDelta   = new Vector2(squareSize, squareSize);
            this.scoresPanel.GreenCandiesImage.GetComponent<RectTransform>().sizeDelta  = new Vector2(squareSize, squareSize);
            this.scoresPanel.OrangeCandiesImage.GetComponent<RectTransform>().sizeDelta = new Vector2(squareSize, squareSize);
            this.scoresPanel.PurpleCandiesImage.GetComponent<RectTransform>().sizeDelta = new Vector2(squareSize, squareSize);
            this.scoresPanel.RedCandiesImage.GetComponent<RectTransform>().sizeDelta    = new Vector2(squareSize, squareSize);
            this.scoresPanel.GoldImage.GetComponent<RectTransform>().sizeDelta          = new Vector2(squareSize, squareSize);

            this.scoresPanel.GoalText.text               = goalText;
            this.scoresPanel.GoalText.fontSize           = this.resolutionManager.SizeForText;
            this.scoresPanel.SwipesRemainsText.fontSize  = this.resolutionManager.SizeForText;
            this.scoresPanel.SwipesRemainsCount.fontSize = this.resolutionManager.SizeForText;

            this.controlPanel.GameControl.GetComponent<RectTransform>().sizeDelta  = this.resolutionManager.SizeForButtons;
            this.controlPanel.MusicControl.GetComponent<RectTransform>().sizeDelta = this.resolutionManager.SizeForButtons;

            this.controlPanel.GameControlText.fontSize  = this.resolutionManager.SizeForText;
            this.controlPanel.MusicControlText.fontSize = this.resolutionManager.SizeForText;

            this.winPanel.TitleText.fontSize   = this.resolutionManager.SizeForTitleText;
            this.loosePanel.TitleText.fontSize = this.resolutionManager.SizeForTitleText;

            this.winPanel.RestartText.fontSize   = this.resolutionManager.SizeForText;
            this.loosePanel.RestartText.fontSize = this.resolutionManager.SizeForText;
            this.loosePanel.HeaderText.fontSize  = this.resolutionManager.SizeForText;

            this.winPanel.RestartButton.GetComponent<RectTransform>().sizeDelta   = this.resolutionManager.SizeForButtons;
            this.loosePanel.RestartButton.GetComponent<RectTransform>().sizeDelta = this.resolutionManager.SizeForButtons;
             string themeText = $"Theme: {this.gameOptions.LevelTheme}";
        // Добавьте отображение темы уровня в UI
        }

        private void ClearUI() {
            this.scoresPanel.BlueScores.text   = "0";
            this.scoresPanel.GreenScores.text  = "0";
            this.scoresPanel.OrangeScores.text = "0";
            this.scoresPanel.PurpleScores.text = "0";
            this.scoresPanel.RedScores.text    = "0";
            this.scoresPanel.GoldScores.text   = "0";

            this.scoresPanel.SwipesRemainsCount.text = this.gameOptions.Swipes.ToString();
        }

        private void OnCandyDestroyed((CandyColor color, CandyType type) valueTuple) {
            this.levelDataManager.CandiesIncrease(valueTuple);
            this.audioManager.OnCandyDestroyed(valueTuple);
        }

        private void RestartTheGame() {
            this.isGameOver = false;
            this.ClearUI();
            this.SwipesRemainsCount.Value = this.gameOptions.Swipes;

            this.winPanel.gameObject.SetActive(false);
            this.loosePanel.gameObject.SetActive(false);

            this.levelDataManager.EmptyTheScores();
            this.InitUI();

            Time.timeScale = 1;
            this.matrixController.Restart();
            this.audioManager.StartPlaying();
        }

        private IEnumerator WinTheGame() {
            this.isGameOver                        = true;
            this.matrixController.IsGameOver.Value = true;

            yield return new WaitForSeconds(.7f);

            this.winPanel.gameObject.SetActive(true);
            this.audioManager.StopPlayingMusic();
            this.audioManager.PlayWinGame();
        }

        private IEnumerator LooseTheGame() {
            this.isGameOver                        = true;
            this.matrixController.IsGameOver.Value = true;

            yield return new WaitForSeconds(.8f);

            this.loosePanel.gameObject.SetActive(true);
            this.audioManager.StopPlayingMusic();
            this.audioManager.PlayLooseGame();
        }

        private void SplitCandiesConfigsFromOptionsToMatrixControllerByCandyType() {
            this.candiesOptions.Pools.ForEach(poolConfig => {
                if (poolConfig.CandyType == CandyType.Simple) {
                    this.matrixController.simpleCandies.Add(new CandyConfig(poolConfig.CandyColor, poolConfig.CandyType));
                }
            });

            this.candiesOptions.Pools.ForEach(poolConfig => {
                if (poolConfig.CandyType == CandyType.Bomb) {
                    this.matrixController.bombCandies.Add(new CandyConfig(poolConfig.CandyColor, poolConfig.CandyType));
                }
            });

            this.candiesOptions.Pools.ForEach(poolConfig => {
                if (poolConfig.CandyType == CandyType.StripedHor) {
                    this.matrixController.strippedHorCandies.Add(new CandyConfig(poolConfig.CandyColor, poolConfig.CandyType));
                }
            });

            this.candiesOptions.Pools.ForEach(poolConfig => {
                if (poolConfig.CandyType == CandyType.StripedVert) {
                    this.matrixController.strippedVertCandies.Add(new CandyConfig(poolConfig.CandyColor, poolConfig.CandyType));
                }
            });

            this.matrixController.colourBomb = this.candiesOptions.Pools
                                                   .Where(poolConfig => poolConfig.CandyColor == CandyColor.Multiple && poolConfig.CandyType == CandyType.Bomb)
                                                   .Select(poolConfig => new CandyConfig(poolConfig.CandyColor, poolConfig.CandyType)).First();
        }

        private void EstablishObservationForDesirableCandyCollected() {
            switch (this.gameOptions.DesiredColorToHave) {
                case CandyColor.Blue:
                    this.ObservableColourCount = this.levelDataManager.BlueCandiesCount;

                    break;

                case CandyColor.Green:
                    this.ObservableColourCount = this.levelDataManager.GreenCandiesCount;

                    break;

                case CandyColor.Orange:
                    this.ObservableColourCount = this.levelDataManager.OrangeCandiesCount;

                    break;

                case CandyColor.Purple:
                    this.ObservableColourCount = this.levelDataManager.PurpleCandiesCount;

                    break;

                case CandyColor.Red:
                    this.ObservableColourCount = this.levelDataManager.RedCandiesCount;

                    break;
            }

            this.ObservableColourCount
                .Where(value => value >= this.gameOptions.DesiredAmount)
                .Where(_ => !this.isGameOver)
                .Subscribe(_ => this.StartCoroutine(this.WinTheGame()));
        }
    }
}