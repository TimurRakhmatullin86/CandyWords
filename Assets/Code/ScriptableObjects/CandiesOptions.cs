namespace ScriptableObjects {
    using System.Collections.Generic;
    using ObjectPool;
    using UnityEngine;

    [CreateAssetMenu(fileName = "CandiesOptions")]
    public class CandiesOptions : ScriptableObject {
        public List<PoolInstance> Pools;

        private void OnValidate() {
            for (int i = 0; i < this.Pools.Count; i++) {
                this.Pools[i].name = this.Pools[i].prefab.name;
            }
        }
    }
}