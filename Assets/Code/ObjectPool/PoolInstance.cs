namespace ObjectPool {
    using System;
    using Structures;

    [Serializable]
    public class PoolInstance {
        public string     name;
        public Pool       pool;
        public CandyColor CandyColor;
        public CandyType  CandyType;
        public int        count;
        public PoolObject prefab;
    }
}