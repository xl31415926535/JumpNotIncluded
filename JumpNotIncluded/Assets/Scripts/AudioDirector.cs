using UnityEngine;

namespace JumpNotIncluded
{
    public class AudioDirector : MonoBehaviour
    {
        public SceneRoot game;
        public AudioSource music,world,ui;
        private bool musicPaused;
        private AudioClip levelMusic,starMusic;
        private int levelSample;
        private GameEvents channel;
        public bool MusicEnabled => music.volume>0;
        public bool EffectsEnabled => world.volume>0;
        public void Init(SceneRoot root)
        {
            game=root;
            music=Source("Music",game.assets.musicGroup);
            world=Source("World SFX",game.assets.worldGroup);
            ui=Source("UI and ads",game.assets.uiGroup);
            music.loop=true;
            music.volume=PlayerPrefs.GetFloat("jni.music",.26f);
            world.volume=ui.volume=PlayerPrefs.GetFloat("jni.effects",.5f);
            AudioListener.volume=1;
            levelMusic=game.assets.Clip(game.world==2?"underground":"theme");starMusic=game.assets.Clip("star");
            music.clip=levelMusic;
            if(game.world>0)music.Play();
            channel=game.events;channel.SoundRequested+=Play;
        }
        private AudioSource Source(string label,UnityEngine.Audio.AudioMixerGroup group)
        {
            var go=new GameObject(label);go.transform.SetParent(transform,false);
            var source=go.AddComponent<AudioSource>();
            source.playOnAwake=false;source.spatialBlend=0;source.outputAudioMixerGroup=group;
            return source;
        }
        public void Play(string cue)
        {
            // Gameplay cues never start an extra copy of the background music.
            if(cue=="theme"||cue=="underground"||cue=="star")return;
            var clip=game.assets.Clip(cue);
            if(clip==null) return;
            bool isUI=cue=="pause"||cue=="death"||cue=="complete";
            (isUI?ui:world).PlayOneShot(clip);
        }
        public void ToggleMusic()
        {music.volume=MusicEnabled?0:.26f;PlayerPrefs.SetFloat("jni.music",music.volume);PlayerPrefs.Save();}
        public void ToggleEffects()
        {world.volume=ui.volume=EffectsEnabled?0:.5f;PlayerPrefs.SetFloat("jni.effects",world.volume);PlayerPrefs.Save();}
        private void Update()
        {
            if(game==null) return;
            bool paused=game.mode==ScreenMode.Pause||game.mode==ScreenMode.Dead||game.mode==ScreenMode.Results;
            bool invincible=game.player!=null&&game.player.buffs.Value==Buff.Star;
            var wanted=invincible&&starMusic!=null?starMusic:levelMusic;
            if(wanted!=null&&music.clip!=wanted)
            {
                // Resume the level track where the star interrupted it. Refreshing a star keeps its music playing.
                if(music.clip==levelMusic)levelSample=music.timeSamples;
                music.clip=wanted;
                music.timeSamples=wanted==levelMusic?Mathf.Clamp(levelSample,0,wanted.samples-1):0;
                if(game.world>0){music.Play();if(paused)music.Pause();}
            }
            if(paused&&!musicPaused)music.Pause();
            else if(!paused&&musicPaused)music.UnPause();
            musicPaused=paused;
            if(game.assets.mixer!=null)
                game.assets.mixer.SetFloat("ShopLowpass",game.mode==ScreenMode.Shop||game.mode==ScreenMode.Ad?850:22000);
        }
        private void OnDestroy(){if(channel!=null)channel.SoundRequested-=Play;}
    }
}
