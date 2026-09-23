using System;
using UnityEngine;

namespace JumpNotIncluded
{
    [CreateAssetMenu(menuName="Jump Not Included/Event Channel")]
    public class GameEvents : ScriptableObject
    {
        public event Action<string,int> EnemyDefeated;
        public event Action<int> ScoreChanged;
        public event Action<string> SoundRequested;
        public void Defeat(string id,int points) => EnemyDefeated?.Invoke(id,points);
        public void Score(int value) => ScoreChanged?.Invoke(value);
        public void Sound(string key) => SoundRequested?.Invoke(key);
    }
}
