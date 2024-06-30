namespace Structures {
    using System;
    using UnityEngine.EventSystems;
    using UnityEngine.UI;
    using UnityEngine;

    public class Cell : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler {
        public Candy Content;
        public Image Image;
        public CellType CellType => this.cellType;
        public int Id => this.id;

        public event Action PointerDownEvent;
        public event Action PointerUpEvent;
        public event Action PointerEnterEvent;
        
        public (Cell First, Cell Second) NeighborsUp;
        public (Cell First, Cell Second) NeighborsDown;
        public (Cell First, Cell Second) NeighborsLeft;
        public (Cell First, Cell Second) NeighborsRight;
        
        [HideInInspector] public RectTransform RectTransform;
        
        private int id;

        [SerializeField] private CellType cellType;

        private (Cell First, Cell Second)[] neighbors;

        private void Awake() => this.RectTransform = this.GetComponent<RectTransform>();

        public void Initialize(int id) => this.id = id;

        public (Cell First, Cell Second)[] Neighbors
            => this.neighbors ?? (this.neighbors = new (Cell first, Cell second)[] {this.NeighborsUp, this.NeighborsDown, this.NeighborsLeft, this.NeighborsRight});

        public void OnPointerDown(PointerEventData eventData) => this.PointerDownEvent?.Invoke();

        public void OnPointerUp(PointerEventData eventData) => this.PointerUpEvent?.Invoke();

        public void OnPointerEnter(PointerEventData eventData) => this.PointerEnterEvent?.Invoke();

        private void OnValidate() {
            var color = this.Image.color;

            switch (this.cellType) {
                case CellType.Open:
                    color.a = 50;

                    break;

                case CellType.Closed:
                    color.a = 0;

                    break;
            }

            this.Image.color = color;
        }
    }
}