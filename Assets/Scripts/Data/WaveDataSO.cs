using System;
using System.Collections.Generic;
using UnityEngine;

namespace FantasyDefense.Data
{
    [Serializable]
    public class WaveSpawnGroup
    {
        public MonsterDataSO monsterData;
        public int count = 5;
        public float spawnInterval = 1.0f;
        public float initialDelay = 0.5f;
    }

    [CreateAssetMenu(fileName = "NewWaveData", menuName = "FantasyDefense/Wave Data", order = 3)]
    public class WaveDataSO : ScriptableObject
    {
        [Header("Wave Configuration")]
        public int waveNumber = 1;
        public string waveTitle = "Wave 1: Serbuan Goblin";
        public List<WaveSpawnGroup> spawnGroups = new List<WaveSpawnGroup>();
        public float restTimeAfterWave = 5.0f;
        public int bonusGemsOnClear = 25;
        [Tooltip("Bonus langkah taktis yang diberikan untuk fase persiapan sebelum gelombang berikutnya")]
        public int bonusMovesOnClear = 5;
    }
}
