namespace Controllers {
    using UniRx;
    using System.Collections;
    using Managers;
    using System;
    using System.Threading.Tasks;
    using DG.Tweening.Core;
    using DG.Tweening.Plugins.Options;
    using DG.Tweening;
    using System.Linq;
    using System.Collections.Generic;
    using ScriptableObjects;
    using Structures;
    using UnityEngine;
    using UnityEngine.UI;
    using Random = UnityEngine.Random;
    using TMPro;

    public class MatrixController : MonoBehaviour {
        public static MatrixController Instance;

        public Cell[] Cells;

        private bool canDisplayNewWord = true;
        private bool isUserAction = false;

        [SerializeField] private WordDatabase wordDatabase;
        [SerializeField] private TextMeshProUGUI wordDisplayText;
        [SerializeField] private TextMeshProUGUI translationDisplayText;

        //It is for Player progress logic
        // private PlayerProgress playerProgress = new PlayerProgress();

        [HideInInspector] public List<CandyConfig> simpleCandies       = new List<CandyConfig>();
        [HideInInspector] public List<CandyConfig> bombCandies         = new List<CandyConfig>();
        [HideInInspector] public List<CandyConfig> strippedHorCandies  = new List<CandyConfig>();
        [HideInInspector] public List<CandyConfig> strippedVertCandies = new List<CandyConfig>();
        [HideInInspector] public CandyConfig       colourBomb;

        public event Action<(CandyColor color, CandyType type)> OnCandyDestroyed;
        public event Action<(CandyColor color, CandyType type)> OnBombCreated;
        public event Action                                     OnSuccesfullSwipe;
        public event Action                                     OnSwiped;

        [HideInInspector] public GameOptions GameOptions;

        private GridLayoutGroup grid;

        [HideInInspector] public bool                   IsOnPause;
        [HideInInspector] public ReactiveProperty<bool> IsGameOver = new ReactiveProperty<bool>();

        private bool isSwipedBeforeFirstFalling;

        private Canvas                      canvas;
        private Cell[][]                    matrix;
        private ReactiveProperty<TurnPhase> turnPhase = new ReactiveProperty<TurnPhase>(TurnPhase.Waiting);

        private int colSize;
        private int rowSize;
        private int clickedCellId;
        private int colIdSize;

        private float swapCandyOffsetX;
        private float swapCandyOffsetY;

        private List<Cell>        bestCaseOfMatchingForSuggestion     = new List<Cell>(8);
        private List<Cell>        destroyedCellsForFallingCalculation = new List<Cell>();
        private List<MatchedCase> finalMatchedCandidates              = new List<MatchedCase>(16);

        private Vector2 defaultCandyPositionOffset = Vector2.zero;

        private List<TweenerCore<Color, Color, ColorOptions>> suggestTweens = new List<TweenerCore<Color, Color, ColorOptions>>(5);
        private float sizeForCell;

        private void Awake() {
            Instance  = this;
            this.grid = this.GetComponent<GridLayoutGroup>();

            this.canvas = this.GetComponentInParent<Canvas>();

            this.IsGameOver.Subscribe(value => {
                if (value) {
                    this.UnsetEffectsForCandiesInCells();
                }
            });
        }

        public void Initialize(float sizeOfCell) {
            this.sizeForCell = sizeOfCell;
            this.grid.cellSize = new Vector2(this.sizeForCell, this.sizeForCell);
            
            this.colSize   = this.grid.constraintCount;
            this.rowSize   = this.grid.constraintCount;
            this.colIdSize = this.colSize - 1;

            this.swapCandyOffsetX = this.grid.cellSize.x + this.grid.spacing.x;
            this.swapCandyOffsetY = this.grid.cellSize.y + this.grid.spacing.y;

            this.grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            this.matrix          = new Cell[this.colSize][];

            this.InitializeTheMatrix();

            this.turnPhase
                .Where(value => value == TurnPhase.Waiting)
                .Subscribe(_ => this.FindBestCaseProcess(this.GameOptions.SuggestTimer, true));

            this.FindBestCaseProcess(this.GameOptions.SuggestTimer, true);
            this.turnPhase.Value = TurnPhase.Waiting;
        }

        public void Restart() {
            canDisplayNewWord = true;
            this.destroyedCellsForFallingCalculation.Clear();
            this.finalMatchedCandidates.Clear();
            this.MixAllCandiesInCells();

            this.IsGameOver.Value = false;
            this.FindBestCaseProcess(this.GameOptions.SuggestTimer, true);
        }

        private async void FindBestCaseProcess(float delay, bool doesEffectsNeeded) {
            this.finalMatchedCandidates.Clear();

            if (delay <= 0) {
                delay = 1;
            }

            await Task.Delay(TimeSpan.FromSeconds(delay));

            this.finalMatchedCandidates = this.FindMatchingListsOfCells(true);

            int count = 0;

            this.finalMatchedCandidates.ForEach(FindBestCaseForSuggestion);

            void FindBestCaseForSuggestion(MatchedCase matchedCase) {
                if (matchedCase.Candidates.Count > count) {
                    count                                = matchedCase.Candidates.Count;
                    this.bestCaseOfMatchingForSuggestion = matchedCase.Candidates;
                }
            }

            while (this.bestCaseOfMatchingForSuggestion.Count == 0) {
                this.MixAllCandiesInCells();
                this.finalMatchedCandidates = this.FindMatchingListsOfCells(true);
                this.finalMatchedCandidates.ForEach(FindBestCaseForSuggestion);
            }

            if (this.bestCaseOfMatchingForSuggestion.Count == 3) {
                var randomNumber = Random.Range(0, this.finalMatchedCandidates.Count);
                this.bestCaseOfMatchingForSuggestion = this.finalMatchedCandidates[randomNumber].Candidates;
            }

            if (doesEffectsNeeded) {
                this.StartEffectsProcess(this.bestCaseOfMatchingForSuggestion);
            }
        }

        private void StartEffectsProcess(List<Cell> bestCaseOfMatchingForSuggestion) {
            this.DoEffectsForCandiesInCells(bestCaseOfMatchingForSuggestion);
        }

        private void DoEffectsForCandiesInCells(List<Cell> cells) {
            if (cells.Count > 0) {
                cells.ForEach(cell => {
                    var image = cell.Content.Image;

                    var tween = image.DOFade(0.3f, .7f).OnComplete(() => image.DOFade(1, 1f)).SetLoops(-1, LoopType.Yoyo);

                    this.suggestTweens.Add(tween);
                });
            }
        }

        private void UnsetEffectsForCandiesInCells() {
            this.suggestTweens.ForEach(tween => tween.Kill());
            this.suggestTweens.Clear();

            foreach (var cell in this.Cells) {
                if (cell.Content != null) {
                    var image = cell.Content.Image;

                    image.DOFade(1, 0);
                }
            }

            this.bestCaseOfMatchingForSuggestion.Clear();
        }

        private void InitializeTheMatrix() {
            for (int i = 0; i < this.Cells.Length; i++) {
                if (i < this.colSize) {
                    this.matrix[i] = new Cell[this.rowSize];
                }

                this.Cells[i].Initialize(i);

                var coordinates = this.FromArrayToMatrix(i);
                this.matrix[coordinates.colId][coordinates.rowId] = this.Cells[i];

                this.PutCandyInCell(i);

                var index = i;

                this.Cells[index].PointerUpEvent    += this.OnPointerUp;
                this.Cells[index].PointerDownEvent  += () => this.OnPointerDown(index);
                this.Cells[index].PointerEnterEvent += () => this.OnPointerEnter(index); // when swipe trespassed another cell
            }

            for (int i = 0; i < this.matrix.Length; i++) {
                for (int j = 0; j < this.matrix[i].Length; j++) {
                    if (j - 1 >= 0) {
                        // if not first row
                        this.matrix[i][j].NeighborsUp.First = this.matrix[i][j - 1]; // add upper

                        if (j - 2 >= 0) {
                            this.matrix[i][j].NeighborsUp.Second = this.matrix[i][j - 2];
                        }
                    }

                    if (j + 1 <= this.matrix[i].Length - 1) {
                        // if not last row
                        this.matrix[i][j].NeighborsDown.First = this.matrix[i][j + 1]; // add lower

                        if (j + 2 <= this.matrix[i].Length - 1) {
                            this.matrix[i][j].NeighborsDown.Second = this.matrix[i][j + 2];
                        }
                    }

                    if (i - 1 >= 0) {
                        // if not first col
                        this.matrix[i][j].NeighborsLeft.First = this.matrix[i - 1][j]; // add lefter

                        if (i - 2 >= 0) {
                            this.matrix[i][j].NeighborsLeft.Second = this.matrix[i - 2][j];
                        }
                    }

                    if (i + 1 <= this.matrix[i].Length - 1) {
                        // if not last col
                        this.matrix[i][j].NeighborsRight.First = this.matrix[i + 1][j]; // add righter

                        if (i + 2 <= this.matrix[i].Length - 1) {
                            this.matrix[i][j].NeighborsRight.Second = this.matrix[i + 2][j];
                        }
                    }
                }
            }
        }

        private void PutCandyInCell(int cellId) {
            var coordinates  = this.FromArrayToMatrix(cellId);
            var randomNumber = this.SafelyCreateRandomNumber(coordinates, this.simpleCandies);
            var candy        = PoolManager.GetObject(this.simpleCandies[randomNumber], this.Cells[cellId].transform, this.sizeForCell).GetComponent<Candy>();
            candy.RectTransform.anchoredPosition = this.defaultCandyPositionOffset;

            this.Cells[cellId].Content = candy;
        }

        public void MixAllCandiesInCells() {
            this.UnsetEffectsForCandiesInCells();

            for (int i = 0; i < this.Cells.Length; i++) {
                if (this.Cells[i].Content != null) {
                    this.Cells[i].Content.ReturnToPool();
                }

                this.PutCandyInCell(i);
            }
        }

        private void OnPointerDown(int index) {
            if (this.turnPhase.Value != TurnPhase.OnSwitchingProcess) {
                this.turnPhase.Value = TurnPhase.OnClicked;
                this.clickedCellId   = index;
            }

            this.UnsetEffectsForCandiesInCells();
        }

        private void OnPointerUp() {
            if (this.turnPhase.Value != TurnPhase.OnSwitchingProcess) {
                this.turnPhase.Value = TurnPhase.Waiting;
                this.clickedCellId   = -1;
            }
        }

        private IEnumerator SwapBombsSuccesfull(Cell from, Cell destination, List<Cell> cellsToDestroy) {
            var swipedDirection = this.CalculateDirectionFromSwipe(from.Id, destination.Id);

            var swapPromise = this.SwapCandiesUI(from.Content, destination.Content, swipedDirection);

            yield return swapPromise.WaitForCompletion();

            this.DestroyCandyObjectsInCells(cellsToDestroy);

            this.StartCoroutine(this.FallingRoutine());
        }

        private void OnPointerEnter(int swipedIndex) {
            if (this.IsOnPause || this.IsGameOver.Value) {
                return;
            }

            if (this.clickedCellId >= 0 && swipedIndex != this.clickedCellId && this.turnPhase.Value == TurnPhase.OnClicked) {
                var isSwipedCellNeighborOfClicked = this.Cells[this.clickedCellId].Neighbors.Any(cellPair => cellPair.First == this.Cells[swipedIndex]);

                if (!isSwipedCellNeighborOfClicked) {
                    return;
                }
                
                this.turnPhase.Value = TurnPhase.OnSwitchingProcess;

                if (this.CheckIfNotDiagonalCellClicked(swipedIndex)
                 && this.CheckCellForValid(this.Cells[this.clickedCellId])
                 && this.CheckCellForValid(this.Cells[swipedIndex])) {
                    isUserAction = true;
                    canDisplayNewWord = true;
                    var firstCandy  = this.Cells[this.clickedCellId].Content;
                    var secondCandy = this.Cells[swipedIndex].Content;

                    this.OnSwiped?.Invoke();

                    List<Cell> cellsToDestroy = new List<Cell>();

                    // C H E C K    P O I N T
                    this.isSwipedBeforeFirstFalling = true;

                    if (firstCandy.CandyColor == CandyColor.Multiple ||
                        secondCandy.CandyColor == CandyColor.Multiple) {
                        // F I R S T    E N T R Y    P O I N T    TO    S U C C E S S F U L    S W I P E

                        if (firstCandy.CandyColor == CandyColor.Multiple) {
                            // TODO Make Mixed-Bomb Blowing Method: Multiple Bomb + Striped/ Multiple Bomb + Simple Bomb


                            if (secondCandy.CandyColor != CandyColor.Multiple) {
                                cellsToDestroy.AddRange(this.Cells.Where(cell => cell.Content.CandyColor == secondCandy.CandyColor)
                                                            .Select(cell => cell));

                                cellsToDestroy.Add(this.Cells[this.clickedCellId]);
                                cellsToDestroy.Add(this.Cells[swipedIndex]);

                                this.StartCoroutine(this.SwapBombsSuccesfull(this.Cells[this.clickedCellId], this.Cells[swipedIndex], cellsToDestroy));
                            }
                            else if (secondCandy.CandyColor == CandyColor.Multiple) {
                                cellsToDestroy.AddRange(this.Cells);
                                cellsToDestroy.Add(this.Cells[this.clickedCellId]);
                                cellsToDestroy.Add(this.Cells[swipedIndex]);

                                this.StartCoroutine(this.SwapBombsSuccesfull(this.Cells[this.clickedCellId], this.Cells[swipedIndex], cellsToDestroy));
                            }
                        }
                        else {
                            cellsToDestroy.AddRange(this.Cells
                                                        .Where(cell => cell.Content.CandyColor == firstCandy.CandyColor)
                                                        .Select(cell => cell));

                            cellsToDestroy.Add(this.Cells[swipedIndex]);

                            this.StartCoroutine(this.SwapBombsSuccesfull(this.Cells[this.clickedCellId], this.Cells[swipedIndex], cellsToDestroy));
                        }
                    }
                    else if ((firstCandy.CandyType == CandyType.Bomb || firstCandy.CandyType == CandyType.StripedHor || firstCandy.CandyType == CandyType.StripedVert) &&
                             (secondCandy.CandyType == CandyType.Bomb || secondCandy.CandyType == CandyType.StripedHor || secondCandy.CandyType == CandyType.StripedVert)) {
                        // S E C O N D    E N T R Y    P O I N T    TO    S U C C E S S F U L    S W I P E

                        // TODO Make Mixed-Bomb Blowing Method: Bomb + Striped/ Striped + Striped/ Bomb - Bomb
                        cellsToDestroy.AddRange(this.CoolectTheTargetsFromPotentionalExploding(firstCandy.CandyType, this.Cells[this.clickedCellId].Id));
                        cellsToDestroy.AddRange(this.CoolectTheTargetsFromPotentionalExploding(secondCandy.CandyType, this.Cells[swipedIndex].Id));

                        this.StartCoroutine(this.SwapBombsSuccesfull(this.Cells[this.clickedCellId], this.Cells[swipedIndex], cellsToDestroy));
                    }
                    else {
                        this.StartCoroutine(this.CheckCellForMatchingContentFromSwipe(swipedIndex));
                    }
                }
                else {
                    this.CantMatchProcess();
                }
            }
            
        }

        private void CantMatchProcess() {
            this.turnPhase.Value = TurnPhase.Waiting;
            this.PlayUnacceptableCondition();
        }

        private void PlayUnacceptableCondition() {
            // play sound unacceptable
        }

        private bool CheckIfNotDiagonalCellClicked(int index)
            => index + 2 + this.colIdSize != this.clickedCellId && index + this.colIdSize != this.clickedCellId &&
               index - 2 - this.colIdSize != this.clickedCellId && index - this.colIdSize != this.clickedCellId;

        private Direction CalculateDirectionFromSwipe(int cellFromID, int cellToID) {
            var coordinatesFrom = this.FromArrayToMatrix(cellFromID);
            var coordinatesTo   = this.FromArrayToMatrix(cellToID);

            if (coordinatesTo.rowId < coordinatesFrom.rowId) {
                return Direction.Up;
            }
            else if (coordinatesTo.rowId > coordinatesFrom.rowId) {
                return Direction.Down;
            }
            else if (coordinatesTo.colId < coordinatesFrom.colId) {
                return Direction.Left;
            }
            else if (coordinatesTo.colId > coordinatesFrom.colId) {
                return Direction.Right;
            }

            return Direction.Up;
        }

        private IEnumerator CheckCellForMatchingContentFromSwipe(int targetCellId) {
            isUserAction = true;
            var swipedDirection = this.CalculateDirectionFromSwipe(this.clickedCellId, targetCellId);

            var matchedCase        = this.CheckCellsForMatchingByDirection(targetCellId, this.clickedCellId, this.Cells[this.clickedCellId].Content);
            var matchedCaseReverse = this.CheckCellsForMatchingByDirection(this.clickedCellId, targetCellId, this.Cells[targetCellId].Content);

            if (!matchedCase.Equals(default(MatchedCase)) || !matchedCaseReverse.Equals(default(MatchedCase))) {
                // T H I R D    E N T R Y    P O I N T    TO    S U C C E S S F U L    S W I P E

                // Swap cells content in three layers: UI -> GO -> Data (the order is important)
                var swapPromise = this.SwapCandiesUI(this.Cells[this.clickedCellId].Content, this.Cells[targetCellId].Content, swipedDirection);

                yield return swapPromise.WaitForCompletion();

                this.SwapCandiesGameObjects(this.clickedCellId, targetCellId);
                this.SwapCandiesInDataPool(this.clickedCellId, targetCellId);

                if (!matchedCase.Equals(default(MatchedCase))) {
                    this.MatchTheCandies(matchedCase);
                }

                if (!matchedCaseReverse.Equals(default(MatchedCase))) {
                    this.MatchTheCandies(matchedCaseReverse);
                }

                this.StartCoroutine(this.FallingRoutine());
            }
            else {
                this.SwapCandiesBoomerang(targetCellId, swipedDirection);

                yield return null;
            }
        }

        private IEnumerator FallingRoutine() {
            this.UnsetEffectsForCandiesInCells();
            var colsIdForFallingProcess = this.GetColumnsIdsForFallingProcess(this.destroyedCellsForFallingCalculation);

            yield return this.StartCoroutine(this.StartFallingProcessInColumnsById(colsIdForFallingProcess));

            if (this.IsGameOver.Value) {
                yield return null;
            }
            canDisplayNewWord = true;

            var matchedCasesAfterFalling = this.FindMatchingListsOfCells(false);

            void MatchTheCases() {
                MatchedCase bestCase = default(MatchedCase);

                for (int i = 0; i < matchedCasesAfterFalling.Count; i++) {
                    var nullableCandidates = matchedCasesAfterFalling[i].Candidates.Any(candidate => candidate.Content == null);

                    if (nullableCandidates) {
                        matchedCasesAfterFalling.RemoveAt(i);

                        continue;
                    }

                    if (bestCase.Equals(default(MatchedCase))) {
                        bestCase = matchedCasesAfterFalling[i];
                    }
                    else if (matchedCasesAfterFalling[i].MatchedType == MatchedType.FiveInline) {
                        bestCase = matchedCasesAfterFalling[i];

                        break;
                    }
                    else if (matchedCasesAfterFalling[i].Candidates.Count > bestCase.Candidates.Count) {
                        bestCase = matchedCasesAfterFalling[i];
                    }
                }


                if (!bestCase.Equals(default(MatchedCase))) {
                    this.MatchTheCandies(bestCase);
                }
            }

            if (matchedCasesAfterFalling.Count > 0 && !this.IsGameOver.Value) {
                isUserAction = false;
                while (matchedCasesAfterFalling.Count > 0) {
                    MatchTheCases();
                }

                //TODO add effects
                yield return new WaitForSeconds(.2f);

                this.StartCoroutine(this.FallingRoutine());
            }
            else {
                this.turnPhase.Value = TurnPhase.Waiting;
                
                if (this.isSwipedBeforeFirstFalling) {
                    this.OnSuccesfullSwipe?.Invoke();
                    this.isSwipedBeforeFirstFalling = false;
                }
            }

            yield return null;
        }

    public event System.Action<CandyColor> OnCandyMatched;

        private void MatchTheCandies(MatchedCase matchedCase) {
            var cellsOfCandiesToDestroy = matchedCase.Candidates;

            this.DestroyCandyObjectsInCells(cellsOfCandiesToDestroy);

            // remove target
            matchedCase.Candidates.ForEach(candidate => {
                if (candidate == matchedCase.TargetCell) {
                    this.destroyedCellsForFallingCalculation.Remove(candidate);
                }
            });

            var matchedType = matchedCase.MatchedType;

            if (matchedType == MatchedType.FourVert || matchedType == MatchedType.FourHor || matchedType == MatchedType.FiveCross || matchedType == MatchedType.FiveInline) {
                this.CreateTheBomb(matchedCase);
            }
            else {
                this.destroyedCellsForFallingCalculation.Add(matchedCase.TargetCell);
            }
            DisplayWord(matchedCase.CandyColor);
            OnCandyMatched?.Invoke(matchedCase.CandyColor);
        }

         private void DisplayWord(CandyColor color){
    if (!canDisplayNewWord || !isUserAction) return;

    if (wordDatabase == null)
    {
        Debug.LogError("WordDatabase is not assigned in MatrixController!");
        return;
    }

    var (word, translation) = wordDatabase.GetRandomWord(color);

    if (wordDisplayText != null)
    {
        wordDisplayText.text = word;
    }
    else
    {
        Debug.LogError("WordDisplayText is not assigned in MatrixController!");
    }

    if (translationDisplayText != null)
    {
        translationDisplayText.text = translation;
    }
    else
    {
        Debug.LogError("TranslationDisplayText is not assigned in MatrixController!");
    }

canDisplayNewWord = false;
    isUserAction = false;

        // playerProgress.AddLearnedWord(color, word);
        // Здесь можно добавить логику для отображения прогресса игрока
    }

        private IEnumerator StartFallingProcessInColumnsById(int[] colsForFallingProcess) {
            var fallingPromises = this.DoFallingIteration(colsForFallingProcess);

            for (int i = 0; i < fallingPromises.Count; i++) {
                yield return fallingPromises[i].WaitForCompletion();
            }

            this.destroyedCellsForFallingCalculation.Clear();
        }

        private List<TweenerCore<Vector3, Vector3, VectorOptions>> DoFallingIteration(IEnumerable<int> colsForRearrange) {
            var candidatesForFallingInOneColumn = new List<Candy>();
            var falledTweens                    = new List<TweenerCore<Vector3, Vector3, VectorOptions>>();

            // goes through all needed cols left to right
            foreach (var colId in colsForRearrange) {
                candidatesForFallingInOneColumn.Clear();
                int emptyCellsCount    = 0;
                int firstRowForFalling = -1;

                // goes through all needed rows in certain col
                // from bottom to up
                for (int rowId = this.matrix[colId].Length - 1; rowId >= 0; rowId--) {
                    if (this.matrix[colId][rowId].Content == null) {
                        // in case of first empty cell,
                        // this cell becomes reference point for falling
                        if (firstRowForFalling < 0) {
                            firstRowForFalling = rowId;
                        }

                        emptyCellsCount++;
                    }

                    // we can add existing candidate for falling only after first empty cell
                    // starting from the end of cells list
                    else if (emptyCellsCount > 0) {
                        var candyForFalling = this.matrix[colId][rowId].Content;
                        candyForFalling.RowPositionId = rowId;
                        candidatesForFallingInOneColumn.Add(candyForFalling);
                    }
                }

                for (int i = 0; i < emptyCellsCount; i++) {
                    var randomNumber = Random.Range(0, this.simpleCandies.Count);

                    var startPointPosition = this.matrix[colId][0].transform.position;

                    var coordinates = new Vector2(startPointPosition.x, startPointPosition.y + this.swapCandyOffsetY + i * this.swapCandyOffsetY);

                    var newCandyForFalling = PoolManager.GetObject(this.simpleCandies[randomNumber], this.canvas.transform, this.sizeForCell).GetComponent<Candy>();

                    newCandyForFalling.RowPositionId = -(i + 1);

                    newCandyForFalling.gameObject.transform.position = coordinates;

                    candidatesForFallingInOneColumn.Add(newCandyForFalling);
                }

                var falledTween = this.DoFallingIterationInOneColumn(candidatesForFallingInOneColumn, colId, firstRowForFalling);
                falledTweens.Add(falledTween);
            }

            return falledTweens;
        }

        private TweenerCore<Vector3, Vector3, VectorOptions> DoFallingIterationInOneColumn(List<Candy> candidatesForFalling, int colId, int firstRowIdForFalling) {
            var candyListIterator = 0;

            TweenerCore<Vector3, Vector3, VectorOptions> movingTween = null;

            for (int rowId = firstRowIdForFalling; rowId >= 0; rowId--) {
                var candyForFalling = candidatesForFalling[candyListIterator];
                var cell            = this.matrix[colId][rowId];

                cell.Content = candyForFalling;

                var cellDistance    = rowId - candyForFalling.RowPositionId;
                var fallingDistance = this.swapCandyOffsetY * cellDistance;
                var endPosition     = candyForFalling.transform.position.y - fallingDistance;

                movingTween = candyForFalling.RectTransform
                                             .DOMoveY(endPosition, this.GameOptions.FallingTime)
                                             .OnComplete(() => {
                                                  candyForFalling.transform.SetParent(cell.transform);
                                                  candyForFalling.RectTransform.anchoredPosition = this.defaultCandyPositionOffset;
                                              });

                candyListIterator++;
            }

            return movingTween;
        }

        private int[] GetColumnsIdsForFallingProcess(IEnumerable<Cell> candidatesForFallingProcess)
            => (from candidate in candidatesForFallingProcess
                select this.FromArrayToMatrix(candidate.Id).colId).Distinct().OrderBy(number => number).ToArray();

        private void SwapCandiesGameObjects(int indexFirst, int indexSecond) {
            var candieFromSwitchTransform = this.Cells[indexFirst].Content.gameObject.transform;
            var candieToSwitchOnTransform = this.Cells[indexSecond].Content.gameObject.transform;

            candieFromSwitchTransform.SetParent(null);
            candieToSwitchOnTransform.SetParent(null);

            candieFromSwitchTransform.transform.SetParent(this.Cells[indexSecond].gameObject.transform);
            candieToSwitchOnTransform.transform.SetParent(this.Cells[indexFirst].gameObject.transform);
        }

        private void SwapCandiesBoomerang(int targetCellId, Direction swipedDirection) {
            this.SwapCandidatesBoomerangUI(this.clickedCellId, targetCellId, swipedDirection);
            // sound effects
        }

        private void CreateTheBomb(MatchedCase matchedCase) {
            CandyConfig notSimpleCandy;

            switch (matchedCase.MatchedType) {
                case MatchedType.FourVert:
                    notSimpleCandy = this.strippedVertCandies.FirstOrDefault(candyConfig => candyConfig.CandyColor == matchedCase.CandyColor);

                    break;

                case MatchedType.FourHor:
                    notSimpleCandy = this.strippedHorCandies.FirstOrDefault(candyConfig => candyConfig.CandyColor == matchedCase.CandyColor);

                    break;

                case MatchedType.FiveCross:
                    notSimpleCandy = this.bombCandies.FirstOrDefault(candyConfig => candyConfig.CandyColor == matchedCase.CandyColor);

                    break;

                case MatchedType.FiveInline:
                    notSimpleCandy = this.colourBomb;

                    break;

                default:
                    notSimpleCandy = this.colourBomb;

                    break;
            }

            var candy = PoolManager.GetObject(notSimpleCandy, matchedCase.TargetCell.gameObject.transform, this.sizeForCell).GetComponent<Candy>();

            this.OnBombCreated?.Invoke((candy.CandyColor, candy.CandyType));

            candy.RectTransform.offsetMin = this.defaultCandyPositionOffset;
            candy.RectTransform.offsetMax = this.defaultCandyPositionOffset;

            matchedCase.TargetCell.Content = candy;
        }

        private void SwapCandiesInDataPool(int indexFirst, int indexSecond) {
            var candieToSwitchOn = this.Cells[indexFirst].Content;
            this.Cells[indexFirst].Content  = this.Cells[indexSecond].Content;
            this.Cells[indexSecond].Content = candieToSwitchOn;
        }

        private void DestroyCandyObjectsInCells(List<Cell> cells) {
            for (int i = 0; i < cells.Count; i++) {
                if (cells[i].Content != null) {
                    if (cells[i].Content.CandyType == CandyType.Bomb ||
                        cells[i].Content.CandyType == CandyType.StripedHor ||
                        cells[i].Content.CandyType == CandyType.StripedVert) {
                        var additionalKilledCells = this.CoolectTheTargetsFromPotentionalExploding(cells[i].Content.CandyType, cells[i].Id);
                        cells.AddRange(additionalKilledCells);
                    }

                    if (cells[i].Content != null) {
                        this.destroyedCellsForFallingCalculation.Add(cells[i]);
                        this.OnCandyDestroyed?.Invoke((cells[i].Content.CandyColor, cells[i].Content.CandyType));
                        cells[i].Content.ReturnToPool();
                        cells[i].Content = null;
                    }
                }

                //destroy effects
                //destroy sound
            }
        }

        private List<Cell> CoolectTheTargetsFromPotentionalExploding(CandyType candyType, int bombCellId) {
            var cellsToDestroyFromBomb = new List<Cell>();
            var colId                  = 0;
            var rowId                  = 0;

            switch (candyType) {
                case CandyType.Bomb:
                    cellsToDestroyFromBomb
                       .AddRange(this.Cells[bombCellId].Neighbors
                                     .Where(pare => pare.First != null).Select(pare => pare.First));

                    var coordinates = this.FromArrayToMatrix(bombCellId);
                    colId = coordinates.colId;
                    rowId = coordinates.rowId;

                    if (colId - 1 >= 0 && rowId - 1 >= 0 && this.matrix[colId - 1][rowId - 1].CellType != CellType.Closed) {
                        cellsToDestroyFromBomb.Add(this.matrix[colId - 1][rowId - 1]);
                    }

                    if (colId - 1 >= 0 && rowId + 1 <= this.matrix[colId - 1].Length - 1 && this.matrix[colId - 1][rowId + 1].CellType != CellType.Closed) {
                        cellsToDestroyFromBomb.Add(this.matrix[colId - 1][rowId + 1]);
                    }

                    if (colId + 1 <= this.matrix.Length - 1 && rowId - 1 >= 0 && this.matrix[colId + 1][rowId - 1].CellType != CellType.Closed) {
                        cellsToDestroyFromBomb.Add(this.matrix[colId + 1][rowId - 1]);
                    }

                    if (colId + 1 <= this.matrix.Length - 1 && rowId + 1 <= this.matrix[colId + 1].Length - 1 && this.matrix[colId + 1][rowId + 1].CellType != CellType.Closed) {
                        cellsToDestroyFromBomb.Add(this.matrix[colId + 1][rowId + 1]);
                    }

                    break;

                case CandyType.StripedVert:
                    colId = this.FromArrayToMatrix(bombCellId).colId;

                    for (int i = 0; i < this.matrix[colId].Length; i++) {
                        cellsToDestroyFromBomb.Add(this.matrix[colId][i]);
                    }

                    break;

                case CandyType.StripedHor:
                    rowId = this.FromArrayToMatrix(bombCellId).rowId;

                    for (int i = 0; i < this.matrix.Length; i++) {
                        cellsToDestroyFromBomb.Add(this.matrix[i][rowId]);
                    }

                    break;
            }

            cellsToDestroyFromBomb.Add(this.Cells[bombCellId]);
            return cellsToDestroyFromBomb;
        }

        private TweenerCore<Vector2, Vector2, VectorOptions> SwapCandiesUI(Candy clickedCandy, Candy destinationCandy, Direction direction) {
            var clickedCandyPosition     = clickedCandy.RectTransform.anchoredPosition;
            var destinationCandyPosition = destinationCandy.RectTransform.anchoredPosition;

            var newPositions = this.SetCoordinatesBySwipingDirection(clickedCandyPosition, destinationCandyPosition, direction);

            clickedCandy.RectTransform.DOAnchorPos(newPositions.newClickedPosiiton, this.GameOptions.SwapingTime);

            return destinationCandy.RectTransform.DOAnchorPos(newPositions.newDestinationPosiiton, this.GameOptions.SwapingTime);
        }

        private void SwapCandidatesBoomerangUI(int clickedCellId, int comparedCellId, Direction direction) {
            var clickedCandy     = this.Cells[clickedCellId].Content;
            var destinationCandy = this.Cells[comparedCellId].Content;

            var clickedCandyPosition     = clickedCandy.RectTransform.anchoredPosition;
            var destinationCandyPosition = destinationCandy.RectTransform.anchoredPosition;

            var positions = this.SetCoordinatesBySwipingDirection(clickedCandyPosition, destinationCandyPosition, direction);

            clickedCandy.RectTransform.DOAnchorPos(positions.newClickedPosiiton, this.GameOptions.SwapingTime)
                        .OnComplete(() => clickedCandy.RectTransform.DOAnchorPos(clickedCandyPosition, this.GameOptions.SwapingTime));


            destinationCandy.RectTransform.DOAnchorPos(positions.newDestinationPosiiton, this.GameOptions.SwapingTime)
                            .OnComplete(() => destinationCandy.RectTransform.DOAnchorPos(destinationCandyPosition, this.GameOptions.SwapingTime)
                                                              .OnComplete(() => this.turnPhase.Value = TurnPhase.Waiting));
        }

        private (Vector2 newClickedPosiiton, Vector2 newDestinationPosiiton) SetCoordinatesBySwipingDirection(
            Vector2 clickedCandyPosition, Vector2 destinationCandyPosition, Direction direction) {
            Vector2 newClickedPosiiton;
            Vector2 newDestinationPosiiton;

            switch (direction) {
                case Direction.Up:
                    newClickedPosiiton     = new Vector2(clickedCandyPosition.x, clickedCandyPosition.y + this.swapCandyOffsetY);
                    newDestinationPosiiton = new Vector2(destinationCandyPosition.x, destinationCandyPosition.y - this.swapCandyOffsetY);

                    break;

                case Direction.Down:
                    newClickedPosiiton     = new Vector2(clickedCandyPosition.x, clickedCandyPosition.y - this.swapCandyOffsetY);
                    newDestinationPosiiton = new Vector2(destinationCandyPosition.x, destinationCandyPosition.y + this.swapCandyOffsetY);

                    break;

                case Direction.Left:
                    newClickedPosiiton     = new Vector2(clickedCandyPosition.x - this.swapCandyOffsetX, clickedCandyPosition.y);
                    newDestinationPosiiton = new Vector2(clickedCandyPosition.x + this.swapCandyOffsetX, clickedCandyPosition.y);

                    break;

                case Direction.Right:
                    newClickedPosiiton     = new Vector2(clickedCandyPosition.x + this.swapCandyOffsetX, clickedCandyPosition.y);
                    newDestinationPosiiton = new Vector2(clickedCandyPosition.x - this.swapCandyOffsetX, clickedCandyPosition.y);

                    break;

                default:
                    newClickedPosiiton     = new Vector2(clickedCandyPosition.x, clickedCandyPosition.y + this.swapCandyOffsetY);
                    newDestinationPosiiton = new Vector2(destinationCandyPosition.x, destinationCandyPosition.y - this.swapCandyOffsetY);

                    break;
            }

            return (newClickedPosiiton, newDestinationPosiiton);
        }

        private MatchedCase CheckCellsForMatchingByDirection(int targetCellId, int cellFromId, Candy content) {
            var targetCell        = this.Cells[targetCellId];
            var cellFromDirection = this.Cells[cellFromId];

            var onDirectionCandidates = new Cell[2];

            Direction swipedDirection = Direction.Down;

            if (targetCellId - cellFromId == this.colSize) {
                swipedDirection = Direction.Down;
            }
            else if (cellFromId - targetCellId == this.colSize) {
                swipedDirection = Direction.Up;
            }
            else if (targetCellId - cellFromId == 1) {
                swipedDirection = Direction.Right;
            }
            else if (cellFromId - targetCellId == 1) {
                swipedDirection = Direction.Left;
            }

            switch (swipedDirection) {
                case Direction.Up:
                    onDirectionCandidates = new[] {targetCell.NeighborsUp.First, targetCell.NeighborsUp.Second};

                    break;

                case Direction.Down:
                    onDirectionCandidates = new[] {targetCell.NeighborsDown.First, targetCell.NeighborsDown.Second};

                    break;

                case Direction.Left:
                    onDirectionCandidates = new[] {targetCell.NeighborsLeft.First, targetCell.NeighborsLeft.Second};

                    break;

                case Direction.Right:
                    onDirectionCandidates = new[] {targetCell.NeighborsRight.First, targetCell.NeighborsRight.Second};

                    break;
            }

            var singleCandidates      = this.FindSingleMatchingCandidates(targetCell, cellFromDirection, content);
            var severalCandidatePairs = this.FindSeveralMatchingCandidates(targetCell, cellFromDirection, content);

            if (severalCandidatePairs.Length > 1) {
                var fiveInlineCandidates = severalCandidatePairs
                                          .Where(candidatePair => candidatePair.First != onDirectionCandidates[0])
                                          .SelectMany(candidatePair => new List<Cell> {candidatePair.First, candidatePair.Second})
                                          .ToList();

                if (fiveInlineCandidates.Count == 4) {
                    fiveInlineCandidates.Add(targetCell);

                    return new MatchedCase(fiveInlineCandidates, targetCell, MatchedType.FiveInline, fiveInlineCandidates[0].Content.CandyColor);
                }

                var fiveCrossCandidates = severalCandidatePairs
                                         .SelectMany(candidatePair => new List<Cell> {candidatePair.First, candidatePair.Second})
                                         .ToList();

                fiveCrossCandidates.Add(targetCell);

                return new MatchedCase(fiveCrossCandidates, targetCell, MatchedType.FiveCross, fiveCrossCandidates[0].Content.CandyColor);
            }

            if (severalCandidatePairs.Length == 1) {
                var severalCandidates = severalCandidatePairs
                                       .SelectMany(candidatePair => new List<Cell> {candidatePair.First, candidatePair.Second})
                                       .ToList();

                var inliners = severalCandidates
                              .Where((candidate, i) => candidate == onDirectionCandidates[i])
                              .ToList();

                if (inliners.Count == 2) {
                    if (singleCandidates.Length == 2) {
                        var crossers = inliners;
                        crossers.AddRange(singleCandidates);
                        crossers.Add(targetCell);

                        return new MatchedCase(crossers, targetCell, MatchedType.FiveCross, crossers[0].Content.CandyColor);
                    }

                    severalCandidates.Add(targetCell);

                    return new MatchedCase(severalCandidates, targetCell, MatchedType.Three, severalCandidates[0].Content.CandyColor);
                }

                // if there is 1 pair of same-color candies not on same line
                // and at least 1 single same-color candy
                if (singleCandidates.Length == 1) {
                    var candidateForFourMatch = singleCandidates.FirstOrDefault(candidate => candidate != onDirectionCandidates[0]);

                    // if there is 1 same color candy on another side (not on same line too)
                    if (candidateForFourMatch != null) {
                        severalCandidates.Add(candidateForFourMatch);

                        var matchedType = MatchedType.FourHor;

                        if (swipedDirection == Direction.Up || swipedDirection == Direction.Down) {
                            matchedType = MatchedType.FourVert;
                        }

                        severalCandidates.Add(targetCell);

                        return new MatchedCase(severalCandidates, targetCell, matchedType, severalCandidates[0].Content.CandyColor);
                    }
                }

                // single inlined candy doesn't taken into account
                // while there is a same-colour pair 
                severalCandidates.Add(targetCell);

                return new MatchedCase(severalCandidates, targetCell, MatchedType.Three, severalCandidates[0].Content.CandyColor);
            }

            if (singleCandidates.Length > 0) {
                var singleNotInlineCandidates = singleCandidates.Where(candidate => candidate != onDirectionCandidates[0]).ToList();

                if (singleNotInlineCandidates.Count == 2) {
                    singleNotInlineCandidates.Add(targetCell);

                    return new MatchedCase(singleNotInlineCandidates, targetCell, MatchedType.Three, singleNotInlineCandidates[0].Content.CandyColor);
                }
            }

            return new MatchedCase();
        }

        private Cell[] FindSingleMatchingCandidates(Cell cellTo, Cell cellFrom, Candy Content)
            => cellTo.Neighbors
                     .Where(cell => cell.First != null)
                     .Where(cell => cell.First != cellFrom)
                     .Where(cell => cell.First.Content != null)
                     .Where(cell => cell.First.Content.CandyColor != CandyColor.None)
                      // several candidates are not includes single ones
                     .Where(cell => cell.Second == null || cell.Second.Content == null
                                                        || cell.Second.Content.CandyColor == CandyColor.None
                                                        || !cell.Second.Content.Equals(Content))
                     .Where(cell => cell.First.Content.Equals(Content))
                     .Select(cell => cell.First)
                     .ToArray();

        private (Cell First, Cell Second)[] FindSeveralMatchingCandidates(Cell cellTo, Cell cellFrom, Candy Content)
            => cellTo.Neighbors
                     .Where(cell => cell.First != null && cell.Second != null)
                     .Where(cell => cell.First.Content != null && cell.Second.Content != null)
                     .Where(cell => cell.First != cellFrom)
                     .Where(cell => cell.First.Content.CandyColor != CandyColor.None)
                     .Where(cell => cell.Second.Content.CandyColor != CandyColor.None)
                     .Where(cell => cell.First.Content.Equals(Content)
                                 && cell.Second.Content.Equals(Content))
                     .ToArray();

        private MatchedCase FindPotentialMatchingCaseInCertainDirection(Cell firstCell, Direction direction) {
            Cell secondCell;
            Cell thirdCell;

            (Cell First, Cell Second) OnDirectionThirdCellsNeighbors;

            switch (direction) {
                case Direction.Up:
                    secondCell                     = firstCell.NeighborsUp.First;
                    thirdCell                      = firstCell.NeighborsUp.Second;
                    OnDirectionThirdCellsNeighbors = thirdCell.NeighborsUp;

                    break;

                case Direction.Down:
                    secondCell                     = firstCell.NeighborsDown.First;
                    thirdCell                      = firstCell.NeighborsDown.Second;
                    OnDirectionThirdCellsNeighbors = thirdCell.NeighborsDown;

                    break;

                case Direction.Left:
                    secondCell                     = firstCell.NeighborsLeft.First;
                    thirdCell                      = firstCell.NeighborsLeft.Second;
                    OnDirectionThirdCellsNeighbors = thirdCell.NeighborsLeft;

                    break;

                case Direction.Right:
                    secondCell                     = firstCell.NeighborsRight.First;
                    thirdCell                      = firstCell.NeighborsRight.Second;
                    OnDirectionThirdCellsNeighbors = thirdCell.NeighborsRight;

                    break;

                default:
                    secondCell                     = firstCell.NeighborsUp.First;
                    thirdCell                      = firstCell.NeighborsUp.Second;
                    OnDirectionThirdCellsNeighbors = thirdCell.NeighborsUp;

                    break;
            }

            var candidates = new List<Cell>(5);

            var singleCandidates      = this.FindSingleMatchingCandidates(thirdCell, secondCell, secondCell.Content);
            var severalCandidatePairs = this.FindSeveralMatchingCandidates(thirdCell, secondCell, secondCell.Content);

            var severalMatches = severalCandidatePairs.Length;
            var singleMatches  = singleCandidates.Length;
            var matchedType    = MatchedType.None;

            if (singleMatches > 0 || severalMatches > 0) {
                if (singleMatches > 0) {
                    if (severalMatches > 0) {
                        // best fit is where there are potential same 5 inline candies
                        var twoPotentialMatchesOnSameLine = severalCandidatePairs
                                                           .Where(_ => OnDirectionThirdCellsNeighbors.First != null)
                                                           .Where(_ => OnDirectionThirdCellsNeighbors.Second != null)
                                                           .Where(pair => OnDirectionThirdCellsNeighbors.First.Content.Equals(pair.First.Content))
                                                           .Where(pair => OnDirectionThirdCellsNeighbors.Second.Content.Equals(pair.Second.Content))
                                                           .ToArray();

                        if (twoPotentialMatchesOnSameLine.Length > 0) {
                            severalCandidatePairs = twoPotentialMatchesOnSameLine;
                            matchedType           = MatchedType.FiveInline;
                        }
                        else {
                            matchedType = MatchedType.FiveCross;
                        }

                        // added the first pair of several
                        candidates.Add(singleCandidates[0]);
                        candidates.Add(severalCandidatePairs[0].First);
                        candidates.Add(severalCandidatePairs[0].Second);
                    }
                    else if (singleMatches == 3) {
                        matchedType = MatchedType.FiveCross;
                        candidates.AddRange(singleCandidates);
                    }
                    // for four-match case we need at least two single potential same candies and one of them in swipe-direction
                    else if (singleMatches == 2 && singleCandidates.FirstOrDefault(x => x == OnDirectionThirdCellsNeighbors.First) != null) {
                        Cell fourthMatchingCell = singleCandidates.FirstOrDefault(candidate => candidate.Content.Equals(OnDirectionThirdCellsNeighbors.First.Content));

                        candidates.Add(fourthMatchingCell);
                        candidates.Add(singleCandidates.First(candidate => candidate != fourthMatchingCell));

                        if (direction == Direction.Up || direction == Direction.Down) {
                            matchedType = MatchedType.FourVert;
                        }
                        else {
                            matchedType = MatchedType.FourHor;
                        }
                    }
                    else {
                        matchedType = MatchedType.Three;
                        candidates.Add(singleCandidates[0]);
                    }
                }
                else {
                    // if there is any of several matches -- finish with nearest of several as one
                    matchedType = MatchedType.Three;
                    candidates.Add(severalCandidatePairs[0].First);
                }
            }

            if (candidates.Count > 0) {
                candidates.Add(firstCell);
                candidates.Add(secondCell);
            }

            return new MatchedCase(candidates, thirdCell, matchedType, CandyColor.None);
        }

        private MatchedCase FindActualMatchingCaseInCertainDirection(Cell firstCell, Direction direction) {
            Cell secondCell;
            Cell thirdCell;

            (Cell First, Cell Second) OnDirectionThirdCellsNeighbors;

            switch (direction) {
                case Direction.Up:
                    secondCell                     = firstCell.NeighborsUp.First;
                    thirdCell                      = firstCell.NeighborsUp.Second;
                    OnDirectionThirdCellsNeighbors = thirdCell.NeighborsUp;

                    break;

                case Direction.Down:
                    secondCell                     = firstCell.NeighborsDown.First;
                    thirdCell                      = firstCell.NeighborsDown.Second;
                    OnDirectionThirdCellsNeighbors = thirdCell.NeighborsDown;

                    break;

                case Direction.Left:
                    secondCell                     = firstCell.NeighborsLeft.First;
                    thirdCell                      = firstCell.NeighborsLeft.Second;
                    OnDirectionThirdCellsNeighbors = thirdCell.NeighborsLeft;

                    break;

                case Direction.Right:
                    secondCell                     = firstCell.NeighborsRight.First;
                    thirdCell                      = firstCell.NeighborsRight.Second;
                    OnDirectionThirdCellsNeighbors = thirdCell.NeighborsRight;

                    break;

                default:
                    secondCell                     = firstCell.NeighborsUp.First;
                    thirdCell                      = firstCell.NeighborsUp.Second;
                    OnDirectionThirdCellsNeighbors = thirdCell.NeighborsUp;

                    break;
            }

            if (!thirdCell.Content.Equals(firstCell.Content)) {
                return default;
            }

            var candidates = new List<Cell> {firstCell, secondCell, thirdCell};

            var singleCandidates      = this.FindSingleMatchingCandidates(thirdCell, secondCell, secondCell.Content);
            var severalCandidatePairs = this.FindSeveralMatchingCandidates(thirdCell, secondCell, secondCell.Content);

            var severalMatches = severalCandidatePairs.Length;
            var singleMatches  = singleCandidates.Length;
            var matchedType    = MatchedType.Three;

            if (singleMatches > 0 || severalMatches > 0) {
                if (severalMatches > 0) {
                    // best fit is where there are potential same 5 inline candies
                    var twoPotentialMatchesOnSameLine = severalCandidatePairs
                                                       .Where(_ => OnDirectionThirdCellsNeighbors.First != null)
                                                       .Where(_ => OnDirectionThirdCellsNeighbors.Second != null)
                                                       .Where(pair => OnDirectionThirdCellsNeighbors.First.Content.Equals(pair.First.Content))
                                                       .Where(pair => OnDirectionThirdCellsNeighbors.Second.Content.Equals(pair.Second.Content))
                                                       .ToArray();

                    if (twoPotentialMatchesOnSameLine.Length > 0) {
                        severalCandidatePairs = twoPotentialMatchesOnSameLine;
                        matchedType           = MatchedType.FiveInline;
                    }
                    else {
                        matchedType = MatchedType.FiveCross;
                    }

                    // added the first pair of several
                    candidates.Add(severalCandidatePairs[0].First);
                    candidates.Add(severalCandidatePairs[0].Second);
                }
                else if (singleMatches > 0) {
                    var crossMatchingSingles = singleCandidates.Where(candidate => OnDirectionThirdCellsNeighbors.First == null
                                                                                || !candidate.Content.Equals(OnDirectionThirdCellsNeighbors.First.Content)).ToList();

                    if (crossMatchingSingles.Count == 2) {
                        matchedType = MatchedType.FiveCross;
                        candidates.AddRange(crossMatchingSingles);
                    }
                    else if (singleCandidates.FirstOrDefault(x => x == OnDirectionThirdCellsNeighbors.First) != null) {
                        var fourthMatchingCell = singleCandidates.FirstOrDefault(candidate => candidate.Content.Equals(OnDirectionThirdCellsNeighbors.First.Content));

                        if (fourthMatchingCell != null) {
                            candidates.Add(fourthMatchingCell);

                            if (direction == Direction.Up || direction == Direction.Down) {
                                matchedType = MatchedType.FourVert;
                            }
                            else {
                                matchedType = MatchedType.FourHor;
                            }
                        }
                    }
                }
            }

            return new MatchedCase(candidates, thirdCell, matchedType, firstCell.Content.CandyColor);
        }

        private bool CheckCellForValid(Cell cell) {
            var cellsContent = cell.Content;

            return cellsContent != null && cell.CellType != CellType.Closed &&
                   cellsContent.CandyColor != CandyColor.None &&
                   cellsContent.CandyType != CandyType.None;
        }

        private List<MatchedCase> FindMatchingListsOfCells(bool isPotential) {
            List<MatchedCase> finalCandidates = new List<MatchedCase>();

            for (int i = 0; i < this.matrix.Length; i++) {
                for (int j = 0; j < this.matrix[i].Length; j++) {
                    var firstCell = this.matrix[i][j];

                    if (!this.CheckCellForValid(firstCell)) {
                        continue;
                    }

                    // if cell is inside the row, which before than third to the last one
                    // taking into account the second (incoming point for further verification)
                    if (j + 2 <= this.matrix[i].Length - 1) {
                        // if lower candy has matched with current
                        if (firstCell.Content.Equals(this.matrix[i][j + 1].Content)) {
                            MatchedCase result;

                            if (isPotential) {
                                result = this.FindPotentialMatchingCaseInCertainDirection(firstCell, Direction.Down);
                            }
                            else {
                                result = this.FindActualMatchingCaseInCertainDirection(firstCell, Direction.Down);
                            }

                            if (result.MatchedType != MatchedType.None) {
                                finalCandidates.Add(result);
                            }
                        }
                    }

                    // if cell is inside the row, which more than third one
                    if (j - 2 >= 0) {
                        // if upper candy has matched with current
                        if (firstCell.Content.Equals(this.matrix[i][j - 1].Content)) {
                            MatchedCase result;

                            if (isPotential) {
                                result = this.FindPotentialMatchingCaseInCertainDirection(firstCell, Direction.Up);
                            }
                            else {
                                result = this.FindActualMatchingCaseInCertainDirection(firstCell, Direction.Up);
                            }

                            if (result.MatchedType != MatchedType.None) {
                                finalCandidates.Add(result);
                            }
                        }
                    }

                    // if cell is inside the col, which before than third to the last one
                    if (i + 2 <= this.matrix.Length - 1) {
                        // if righter candy has matched with current
                        if (firstCell.Content.Equals(this.matrix[i + 1][j].Content)) {
                            MatchedCase result;

                            if (isPotential) {
                                result = this.FindPotentialMatchingCaseInCertainDirection(firstCell, Direction.Right);
                            }
                            else {
                                result = this.FindActualMatchingCaseInCertainDirection(firstCell, Direction.Right);
                            }

                            if (result.MatchedType != MatchedType.None) {
                                finalCandidates.Add(result);
                            }
                        }
                    }

                    // if cell is inside the col, which more than third one
                    if (i - 2 >= 0) {
                        // if lefter candy has matched with current
                        if (firstCell.Content.Equals(this.matrix[i - 1][j].Content)) {
                            MatchedCase result;

                            if (isPotential) {
                                result = this.FindPotentialMatchingCaseInCertainDirection(firstCell, Direction.Left);
                            }
                            else {
                                result = this.FindActualMatchingCaseInCertainDirection(firstCell, Direction.Left);
                            }

                            if (result.MatchedType != MatchedType.None) {
                                finalCandidates.Add(result);
                            }
                        }
                    }
                }
            }

            return finalCandidates;
        }

        private int FromMatrixToArray((int colId, int rowId) coordinates)
            => coordinates.rowId * this.colSize + coordinates.colId;

        private (int colId, int rowId) FromArrayToMatrix(int id) {
            int colId = id % this.colSize;
            int rowId = id / this.colSize;

            return (colId, rowId);
        }

        private int SafelyCreateRandomNumber((int colId, int rowId) coordinates, List<CandyConfig> candyConfigs) {
            var horizontalExcludingColor = CandyColor.None;

            if (coordinates.colId > 1) {
                var prevColContent       = this.matrix[coordinates.colId - 1][coordinates.rowId].Content;
                var doublePrevColContent = this.matrix[coordinates.colId - 2][coordinates.rowId].Content;

                if (prevColContent != null && doublePrevColContent != null && prevColContent.Equals(doublePrevColContent)) {
                    horizontalExcludingColor = prevColContent.CandyColor;
                }
            }

            var verticalExcludingColor = CandyColor.None;

            if (coordinates.rowId > 1) {
                var prevRowContent       = this.matrix[coordinates.colId][coordinates.rowId - 1].Content;
                var doublePrevRowContent = this.matrix[coordinates.colId][coordinates.rowId - 2].Content;

                if (prevRowContent != null && doublePrevRowContent != null && prevRowContent.Equals(doublePrevRowContent)) {
                    verticalExcludingColor = prevRowContent.CandyColor;
                }
            }

            var randomNumber = Random.Range(0, candyConfigs.Count);

            while (candyConfigs[randomNumber].CandyColor == horizontalExcludingColor ||
                   candyConfigs[randomNumber].CandyColor == verticalExcludingColor) {
                randomNumber = Random.Range(0, candyConfigs.Count);
            }

            return randomNumber;
        }
    }
}