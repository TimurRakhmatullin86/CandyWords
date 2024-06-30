namespace ObjectPool {
    using UnityEngine;
    using System;

    [AddComponentMenu("Pool/PoolObject")]
    public class PoolObject : MonoBehaviour {
        public event Action<Transform> OnPoolReturned;

        public void ReturnToPool() {
            this.gameObject.SetActive(false);
            this.gameObject.transform.SetParent(null);

            this.OnPoolReturned?.Invoke(this.gameObject.transform);
        }
    }
}