namespace ObjectPool {
    using System.Collections.Generic;
    using UnityEngine;

    [AddComponentMenu("Pool/ObjectPooling")]
    public class Pool {
        private List<PoolObject> objects;
        private Transform        objectsParent;

        public void Initialize(int count, PoolObject sample, Transform objectsParent) {
            this.objects       = new List<PoolObject>();
            this.objectsParent = objectsParent;

            for (int i = 0; i < count; i++) {
                this.AddObject(sample, objectsParent);
            }
        }

        public PoolObject GetObject() {
            for (int i = 0; i < this.objects.Count; i++) {
                if (this.objects[i].gameObject.activeInHierarchy == false) {
                    return this.objects[i];
                }
            }

            this.AddObject(this.objects[0], this.objectsParent);
            this.objects.RemoveAt(0);

            return this.objects[this.objects.Count - 1];
        }

        private void AddObject(PoolObject sample, Transform objectsParent) {
            GameObject poolGameObject;

            if (sample.gameObject.scene.name == null) {
                poolGameObject      = Object.Instantiate(sample.gameObject, objectsParent, true);
                poolGameObject.name = sample.name;
            }
            else {
                poolGameObject = sample.gameObject;
            }

            var poolObject = poolGameObject.GetComponent<PoolObject>();

            poolObject.OnPoolReturned += this.OnObjectReturnedToPool;

            this.objects.Add(poolObject);
            poolGameObject.SetActive(false);
        }

        private void OnObjectReturnedToPool(Transform poolObjectTransform) {
            poolObjectTransform.SetParent(this.objectsParent);
        }
    }
}