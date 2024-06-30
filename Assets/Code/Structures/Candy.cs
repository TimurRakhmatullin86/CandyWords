namespace Structures {
    using DG.Tweening;
    using ObjectPool;
    using System;
    using UnityEngine.UI;
    using UnityEngine;

    public class Candy : PoolObject, IEquatable<Candy> {
        public CandyColor CandyColor => this.candyColor;
        public CandyType  CandyType  => this.candyType;
        public float      RowPositionId;

        [HideInInspector] public RectTransform RectTransform;

        public Image Image;

        [SerializeField] private CandyColor candyColor;
        [SerializeField] private CandyType  candyType;

        public bool Equals(Candy other) => other != null && this.candyColor == other.candyColor;

        private void Awake() {
            this.RectTransform  =  this.GetComponent<RectTransform>();
            this.OnPoolReturned += (Transform trans) => this.Image.DOFade(1, 0);
        }
    }
}