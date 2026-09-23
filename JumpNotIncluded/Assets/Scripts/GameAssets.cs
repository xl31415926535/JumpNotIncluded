using UnityEngine;
using UnityEngine.Audio;

namespace JumpNotIncluded
{
    [System.Serializable] public class NamedSprite { public string key; public Sprite sprite; }
    [System.Serializable] public class NamedAudio { public string key; public AudioClip clip; }
    [CreateAssetMenu(menuName="Jump Not Included/Assets")]
    public class GameAssets : ScriptableObject
    {
        public int revision;
        public NamedSprite[] sprites;
        public NamedAudio[] sounds;
        public Material blueKey, greenKey;
        public Sprite solid;
        public Texture2D sutdAIAd,sutdRobotAd,slCheaterAd;
        public StateDefinition[] formStates, buffStates;
        public ProductDefinition[] products;
        public AudioMixer mixer;
        public AudioMixerGroup musicGroup, worldGroup, uiGroup;
        public Sprite Sprite(string key)
        { foreach(var item in sprites) if(item.key==key) return item.sprite; return solid; }
        public AudioClip Clip(string key)
        { foreach(var item in sounds) if(item.key==key) return item.clip; return null; }
    }
}
