namespace Managers {
    using System.Collections.Generic;
    using ObjectPool;
    using Structures;
    using UnityEngine;

    public class PoolManager : MonoBehaviour {
        public static PoolManager Instance;

        public GameObject PoolParent;

        private static List<PoolInstance> pools;

        private void Awake() => Instance = this;

        public void Initialize(List<PoolInstance> newPools) {
            pools                = newPools;
            this.PoolParent.name = "Pool";

            for (int i = 0; i < pools.Count; i++) {
                if (pools[i].prefab != null) {
                    pools[i].pool = new Pool();
                    pools[i].pool.Initialize(pools[i].count, pools[i].prefab, this.PoolParent.transform);
                }
            }
        }

        public static GameObject GetObject(CandyConfig candyOptions, Transform parent, float rectSize) {
            if (pools != null) {
                for (int i = 0; i < pools.Count; i++) {
                    if (pools[i].CandyColor == candyOptions.CandyColor && pools[i].CandyType == candyOptions.CandyType) {
                        var result = pools[i].pool.GetObject().gameObject;
                        result.transform.SetParent(parent);
                        var rectTransform = result.GetComponent<RectTransform>();
                        rectTransform.sizeDelta = new Vector2(rectSize, rectSize);
                        result.SetActive(true);

                        return result;
                    }
                }
            }

            return null;
        }
    }
}