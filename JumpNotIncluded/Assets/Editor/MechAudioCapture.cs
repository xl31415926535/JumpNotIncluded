using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using JumpNotIncluded;

// Actual Unity mixer output. No clip concatenation, substituted soundtrack or synthetic playback.
[InitializeOnLoad]
public static class MechAudioCapture
{
    const string Pending="jni.mech.audio.capture";
    const int Fps=30,Channels=2;
    static IEnumerator steps;
    static double deadline;
    static int lastFrame,sampleRate,previewFrame;
    static bool recording,previewRecording;
    static float previousCaptureDelta,previousListenerVolume;
    static bool previousListenerPause;
    static AudioConfiguration previousConfiguration;
    static InputSettings originalInput,testInput;
    static Keyboard keyboard;
    static readonly List<float> previewPcm=new List<float>();
    static float lastPeak,lastRms,maxClockDrift;
    static SceneRoot Game=>UnityEngine.Object.FindFirstObjectByType<SceneRoot>();
    static string Output=>Path.GetFullPath("../work/mech-audio-checks.txt");
    static string DirectoryPath=>Path.GetFullPath("../artifacts/mech-audio");

    static MechAudioCapture()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Pending,false))Begin();
            if(state==PlayModeStateChange.ExitingPlayMode&&steps!=null)Finish("Play mode ended before audio capture completed.");
        };
    }
    [MenuItem("Tools/Jump Not Included/Capture mech audio and arrival movie")]
    public static void Start()
    {
        if(steps!=null)return;
        JumpNotIncluded.EditorTools.ProjectSetup.Setup();
        JumpNotIncluded.EditorTools.ProjectSetup.Validate();
        SessionState.SetBool(Pending+".background",Application.runInBackground);
        SessionState.SetBool(Pending+".hadHigh",PlayerPrefs.HasKey("jni.highscore"));
        SessionState.SetInt(Pending+".high",PlayerPrefs.GetInt("jni.highscore",0));
        SessionState.SetString(Pending+".run",JsonUtility.ToJson(Resources.Load<RunState>("RunState")));
        SessionState.SetBool(Pending,true);
        Directory.CreateDirectory(Path.GetDirectoryName(Output));Directory.CreateDirectory(DirectoryPath);
        Directory.CreateDirectory(Path.Combine(DirectoryPath,"frames"));
        File.WriteAllText(Output,"MECH AUDIO QA / Unity AudioRenderer main-output PCM / fixed 30 FPS\n");
        if(EditorApplication.isPlaying)Begin();
        else{EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");EditorApplication.isPlaying=true;}
    }
    static void Begin()
    {
        if(steps!=null)return;
        previousCaptureDelta=Time.captureDeltaTime;previousConfiguration=AudioSettings.GetConfiguration();
        previousListenerVolume=AudioListener.volume;previousListenerPause=AudioListener.pause;
        originalInput=InputSystem.settings;testInput=UnityEngine.Object.Instantiate(originalInput);
        testInput.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        testInput.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings=testInput;keyboard=InputSystem.AddDevice<Keyboard>("JNI audio QA keyboard");
        Application.runInBackground=true;
        steps=Run();deadline=EditorApplication.timeSinceStartup+240;lastFrame=-1;
        try
        {
            var config=previousConfiguration;config.speakerMode=AudioSpeakerMode.Stereo;config.sampleRate=48000;
            Check(AudioSettings.Reset(config),"Unity audio device accepts the stereo capture configuration");
            sampleRate=AudioSettings.outputSampleRate;Time.captureFramerate=Fps;
            AudioListener.volume=1;AudioListener.pause=false;
            Check(AudioRenderer.Start(),"AudioRenderer enters actual main-output capture mode");recording=true;
            EditorApplication.update+=Tick;
        }
        catch(Exception error){Finish(error.ToString());}
    }
    static void Tick()
    {
        if(steps==null)return;
        try
        {
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Audio QA exceeded its deadline.");
            if(lastFrame==Time.frameCount){EditorApplication.QueuePlayerLoopUpdate();return;}
            lastFrame=Time.frameCount;
            RenderMixedFrame();
            if(!steps.MoveNext())Finish(null);
        }
        catch(Exception error){Finish(error.ToString());}
    }
    static void Check(bool condition,string label)
    {if(!condition)throw new Exception(label);File.AppendAllText(Output,"PASS: "+label+"\n");}
    static void Keys(params Key[] keys)=>InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));
    static IEnumerator Frames(int count){for(int i=0;i<count;i++)yield return null;}
    static void Position(PlayerMotor player,Vector2 position)
    {player.body.position=position;player.transform.position=position;player.body.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();}
    static void Focus(SceneRoot game,bool focus)
    {game.SendMessage("OnApplicationFocus",focus,SendMessageOptions.RequireReceiver);}
    static int SampleDistance(int a,int b,int length)
    {int distance=Mathf.Abs(a-b);return Mathf.Min(distance,Mathf.Max(0,length-distance));}

    static void RenderMixedFrame()
    {
        int frames=AudioRenderer.GetSampleCountForCaptureFrame();
        if(frames<=0)throw new Exception("AudioRenderer returned no samples for a capture frame.");
        using(var buffer=new NativeArray<float>(frames*Channels,Allocator.Temp,NativeArrayOptions.UninitializedMemory))
        {
            if(!AudioRenderer.Render(buffer))throw new Exception("Unity failed to render the real mixed audio frame.");
            double energy=0;lastPeak=0;
            for(int i=0;i<buffer.Length;i++)
            {
                float sample=buffer[i];
                if(float.IsNaN(sample)||float.IsInfinity(sample))throw new Exception("Non-finite PCM sample in Unity output.");
                energy+=sample*sample;lastPeak=Mathf.Max(lastPeak,Mathf.Abs(sample));
                if(previewRecording)previewPcm.Add(sample);
            }
            lastRms=(float)Math.Sqrt(energy/buffer.Length);
        }
        if(previewRecording)
        {
            double audioSeconds=previewPcm.Count/(double)(sampleRate*Channels);
            float cinematic=Game.mechArrival!=null?Game.mechArrival.Elapsed:MechArrival.Duration;
            maxClockDrift=Mathf.Max(maxClockDrift,Mathf.Abs((float)audioSeconds-cinematic));
            SaveFrame(++previewFrame,audioSeconds,cinematic);
        }
    }
    static IEnumerator Run()
    {
        Resources.Load<RunState>("RunState").NewRun();SceneManager.LoadScene("World01");
        var wait=Frames(8);while(wait.MoveNext())yield return null;
        var g=Game;g.hasFocus=true;
        foreach(var enemy in UnityEngine.Object.FindObjectsByType<EnemyActor>(FindObjectsSortMode.None))UnityEngine.Object.Destroy(enemy.gameObject);
        g.Run.Credit(RunModel.MechCost);Check(g.Run.BuyMech(),"audio fixture owns a paid suit");
        g.Run.mechDeployed=true;var p=g.player;p.RefreshEquipment();Position(p,new Vector2(2,1.1f));Keys();
        wait=Frames(10);while(wait.MoveNext())yield return null;
        var suit=p.mech;var audio=g.audioDirector;
        audio.music.volume=.26f;audio.world.volume=audio.ui.volume=.5f;
        var bank=new[]{g.assets.mechIgnition,g.assets.mechThrustLoop,g.assets.mechLaser,g.assets.mechLanding,
            g.assets.mechShield,g.assets.mechDescent,g.assets.mechUplink,g.assets.mechReady,g.assets.mechBoost};
        var unique=new HashSet<AudioClip>();
        foreach(var clip in bank){Check(clip!=null&&clip.samples>0&&clip.frequency>0,"8-bit cue is loaded: "+(clip!=null?clip.name:"missing"));unique.Add(clip);}
        Check(unique.Count==9,"all nine mech cues are independently authored clips");
        Check(audio.music.outputAudioMixerGroup!=audio.world.outputAudioMixerGroup&&
            suit.ThrusterSource.outputAudioMixerGroup==g.assets.worldGroup&&suit.WeaponSource.outputAudioMixerGroup==g.assets.worldGroup&&
            suit.StatusSource.outputAudioMixerGroup==g.assets.worldGroup&&suit.IgnitionSource.outputAudioMixerGroup==g.assets.worldGroup,
            "mech audio routes through World SFX independently of background music");
        Check(suit.IsGrounded&&!suit.ThrusterSource.isPlaying,"a grounded suit starts without looping engine sound");

        int ignitionCount=suit.IgnitionsPlayed;Keys(Key.Space);
        wait=Frames(12);while(wait.MoveNext())yield return null;
        Check(suit.IgnitionsPlayed==ignitionCount+1&&suit.ThrusterSource.isPlaying&&suit.ThrusterSource.loop,
            "takeoff triggers ignition once and starts the continuous engine loop");
        Keys();wait=Frames(5);while(wait.MoveNext())yield return null;
        int loopSample=suit.ThrusterSource.timeSamples;float pitch=suit.ThrusterSource.pitch;
        wait=Frames(3);while(wait.MoveNext())yield return null;
        int expected=(loopSample+Mathf.RoundToInt(suit.ThrusterSource.clip.frequency*3f/Fps*pitch))%suit.ThrusterSource.clip.samples;
        Check(SampleDistance(suit.ThrusterSource.timeSamples,expected,suit.ThrusterSource.clip.samples)<suit.ThrusterSource.clip.frequency*.09f,
            "hover engine playhead advances continuously rather than restarting every frame");
        Check(suit.IgnitionsPlayed==ignitionCount+1,"holding flight then releasing thrust does not repeat the ignition cue");

        g.SetMode(ScreenMode.Pause);wait=Frames(2);while(wait.MoveNext())yield return null;
        loopSample=suit.ThrusterSource.timeSamples;
        Check(!suit.ThrusterSource.isPlaying,"pause silences the engine source");
        wait=Frames(6);while(wait.MoveNext())yield return null;
        Check(suit.ThrusterSource.timeSamples==loopSample,"pause preserves the engine sample position");
        g.SetMode(ScreenMode.Playing);wait=Frames(2);while(wait.MoveNext())yield return null;
        Check(suit.ThrusterSource.isPlaying&&suit.IgnitionsPlayed==ignitionCount+1,"resume continues flight without another ignition");
        Focus(g,false);wait=Frames(2);while(wait.MoveNext())yield return null;
        loopSample=suit.ThrusterSource.timeSamples;
        Check(!suit.ThrusterSource.isPlaying,"lost window focus pauses mech audio even when background simulation is enabled");
        wait=Frames(6);while(wait.MoveNext())yield return null;
        Check(suit.ThrusterSource.timeSamples==loopSample,"lost focus does not rewind or advance the paused loop");
        Focus(g,true);wait=Frames(2);while(wait.MoveNext())yield return null;
        Check(suit.ThrusterSource.isPlaying&&suit.IgnitionsPlayed==ignitionCount+1,"regaining focus resumes the same flight without replaying ignition");

        int boosts=suit.BoostsPlayed;Keys(Key.D);wait=Frames(8);while(wait.MoveNext())yield return null;
        Check(suit.BoostsPlayed==boosts+1,"entering horizontal cruise triggers one boost cue");
        wait=Frames(8);while(wait.MoveNext())yield return null;
        Check(suit.BoostsPlayed==boosts+1&&suit.ThrusterSource.isPlaying,"sustained cruise keeps its loop and does not spam boost cues");
        Keys();wait=Frames(4);while(wait.MoveNext())yield return null;
        int shields=suit.ShieldSoundsPlayed;suit.AbsorbHit();wait=Frames(2);while(wait.MoveNext())yield return null;
        Check(suit.ShieldSoundsPlayed==shields+1,"an absorbed monster hit triggers the shield cue");
        wait=Frames(24);while(wait.MoveNext())yield return null;
        float musicVolume=audio.music.volume;var musicClip=audio.music.clip;audio.world.volume=0;audio.ui.volume=0;
        wait=Frames(10);while(wait.MoveNext())yield return null;
        Check(suit.ThrusterSource.volume==0&&suit.StatusSource.volume==0&&suit.WeaponSource.volume==0,
            "the effects control mutes all continuous and one-shot mech channels");
        Check(audio.music.volume==musicVolume&&audio.music.clip==musicClip&&audio.music.isPlaying&&lastRms>0.00001f,
            "muting effects leaves real background music audible and on its own source");
        audio.music.volume=0;wait=Frames(8);while(wait.MoveNext())yield return null;
        Check(lastRms<0.00001f,"muting music and effects produces actual near-silent Unity mixer PCM");
        audio.world.volume=.5f;wait=Frames(8);while(wait.MoveNext())yield return null;
        Check(audio.music.volume==0&&suit.ThrusterSource.volume>0&&lastRms>0.00001f,
            "restoring only effects makes the real engine audible while music remains muted");
        int landings=suit.LandingsPlayed;Keys(Key.S);
        for(int i=0;i<120&&!suit.IsGrounded;i++)yield return null;
        Check(suit.IsGrounded&&suit.LandingsPlayed==landings+1,"touching real ground plays the landing cue once");
        Keys();wait=Frames(20);while(wait.MoveNext())yield return null;
        Check(suit.LandingsPlayed==landings+1&&!suit.ThrusterSource.isPlaying,"remaining on the ground neither loops landing nor leaves the engine running");

        // A separate interruption fixture is not inserted into the delivered continuous preview.
        var prepare=PrepareArrival();while(prepare.MoveNext())yield return null;g=Game;
        wait=Frames(12);while(wait.MoveNext())yield return null;
        var delivery=g.mechArrival;float elapsed=delivery.Elapsed;
        Focus(g,false);wait=Frames(2);while(wait.MoveNext())yield return null;
        AudioSource loop=null;
        foreach(var source in delivery.GetComponentsInChildren<AudioSource>())if(source.loop)loop=source;
        Check(loop!=null&&!delivery.AudioPlaying,"arrival focus loss silences its dedicated audio sources");
        loopSample=loop.timeSamples;
        wait=Frames(8);while(wait.MoveNext())yield return null;
        Check(Mathf.Abs(delivery.Elapsed-elapsed)<.001f&&loop.timeSamples==loopSample,"arrival focus loss freezes both cinematic time and actual audio playhead");
        Focus(g,true);wait=Frames(2);while(wait.MoveNext())yield return null;
        Check(delivery.AudioPlaying&&delivery.Elapsed>elapsed,"arrival focus recovery resumes picture and audio together");
        for(int i=0;i<220&&g.Deploying;i++)yield return null;
        Check(g.Playing&&g.Run.mechDeployed,"the audio interruption fixture completes through the real arrival callback");

        prepare=PrepareArrival();while(prepare.MoveNext())yield return null;g=Game;
        Check(g.Deploying&&g.mechArrival.Elapsed<.001f,"continuous AV capture starts at the actual first arrival frame");
        previewPcm.Clear();previewFrame=0;maxClockDrift=0;
        File.WriteAllText(Path.Combine(DirectoryPath,"frames/timing.csv"),"file,audio_seconds,cinematic_seconds\n");
        SaveFrame(0,0,0);previewRecording=true;
        for(int i=0;i<220&&g.Deploying;i++)yield return null;
        previewRecording=false;
        Check(g.Playing&&g.Run.mechDeployed,"the recorded sequence reaches control through normal gameplay state updates");
        double duration=previewPcm.Count/(double)(sampleRate*Channels);
        Check(Math.Abs(duration-MechArrival.Duration)<=1.6/Fps&&maxClockDrift<=1.6f/Fps,
            "actual audio samples and cinematic clock remain aligned within one capture-frame margin (duration="+duration.ToString("F4",CultureInfo.InvariantCulture)+", drift="+maxClockDrift.ToString("F4",CultureInfo.InvariantCulture)+")");
        int finalSamples=Mathf.RoundToInt(MechArrival.Duration*sampleRate)*Channels;
        Check(previewPcm.Count>=finalSamples,"the real mixed output contains the full 6.2-second arrival interval");
        double energy=0;float peak=0;for(int i=0;i<finalSamples;i++){float s=previewPcm[i];energy+=s*s;peak=Mathf.Max(peak,Mathf.Abs(s));}
        Check(energy/finalSamples>0.00000001&&peak<1,"the recorded arrival mix is audible and does not clip");
        WriteWave(Path.Combine(DirectoryPath,"arrival-mix.wav"),previewPcm,finalSamples);
        File.WriteAllText(Path.Combine(DirectoryPath,"README.md"),
            "# Actual Unity mech audio capture\n\n"+
            "arrival-mix.wav is 6.2 seconds of real stereo Unity AudioRenderer output at "+sampleRate+" Hz, encoded as PCM16. No sound files were concatenated or substituted. A possible final partial capture frame is trimmed after the 6.2-second interval.\n\n"+
            "frames/timing.csv records each game-camera image against both the captured PCM sample clock and MechArrival.Elapsed. Use the CSV's audio_seconds for assembly; the initial frame is timestamp zero. Existing files not referenced in this CSV belong to older runs. The preview was staged with wallet funds and a completed revival ad; it is not a human playthrough or a course acceptance recording.\n\n"+
            "Unity simulation and audio render at 30 FPS. The first fixture independently tested focus loss and pause; the continuous preview contains no inserted pauses. All settings and run data are restored on completion.\n");
        File.AppendAllText(Output,"CAPTURED: arrival-mix.wav / "+sampleRate+" Hz stereo / 6.2 s / "+(previewFrame+1)+" referenced frames\n");
    }

    static IEnumerator PrepareArrival()
    {
        Keys();Resources.Load<RunState>("RunState").NewRun();SceneManager.LoadScene("World01");
        var wait=Frames(8);while(wait.MoveNext())yield return null;
        var g=Game;g.hasFocus=true;g.Run.Credit(RunModel.MechCost);g.SaveCheckpoint(g.session.checkpointPosition);
        g.KillPlayer("Audio presentation fixture.");g.OpenShop();wait=Frames(8);while(wait.MoveNext())yield return null;g=Game;
        Check(g.BuyMech(),"fresh audio presentation fixture purchases its replica through the real shop");
        g.Retry();g.hasFocus=false;
        // Let pre-arrival purchase/UI audio finish in the real mixer without advancing the staged ad.
        wait=Frames(65);while(wait.MoveNext())yield return null;
        g.hasFocus=true;g.AdvanceAd(SceneRoot.ReviveAdDuration);
    }
    static void WriteWave(string path,List<float> samples,int count)
    {
        using(var writer=new BinaryWriter(File.Create(path)))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*2);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);
            writer.Write((short)Channels);writer.Write(sampleRate);writer.Write(sampleRate*Channels*2);
            writer.Write((short)(Channels*2));writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(count*2);
            for(int i=0;i<count;i++)writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(samples[i],-1,1)*32767));
        }
    }
    static void SaveFrame(int index,double audioSeconds,float cinematicSeconds)
    {
        string name="frame_"+index.ToString("D4")+".png";
        Capture(Path.Combine(DirectoryPath,"frames",name),960,540);
        File.AppendAllText(Path.Combine(DirectoryPath,"frames/timing.csv"),name+","+
            audioSeconds.ToString("F6",CultureInfo.InvariantCulture)+","+cinematicSeconds.ToString("F6",CultureInfo.InvariantCulture)+"\n");
    }
    static void Capture(string path,int width,int height)
    {
        var flags=BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.FlattenHierarchy;
        var render=typeof(EditorGUIUtility).GetMethod("RenderPlayModeViewCamerasInternal",flags);
        if(render==null)throw new MissingMethodException("Unity game-view renderer is unavailable.");
        Type type=null;foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies()){type=assembly.GetType("UnityEditor.PlayModeView");if(type!=null)break;}
        if(type==null)throw new MissingMemberException("Unity PlayModeView is unavailable.");
        var view=type.GetMethod("GetMainPlayModeView",flags).Invoke(null,null);
        if(view==null)
        {
            view=ScriptableObject.CreateInstance(type.Assembly.GetType("UnityEditor.GameView"));
            var parent=typeof(EditorWindow).GetField("m_Parent",BindingFlags.Instance|BindingFlags.NonPublic);
            parent.SetValue(view,ScriptableObject.CreateInstance(parent.FieldType));
        }
        type.GetProperty("targetSize",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).SetValue(view,new Vector2(width,height));
        Game.cameraView.pixelRect=new Rect(0,0,width,height);Game.cameraView.aspect=(float)width/height;
        var target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);target.Create();
        var previous=RenderTexture.active;RenderTexture readback=null;Texture2D image=null;
        try
        {
            Event.current=new Event{type=EventType.Repaint};render.Invoke(null,new object[]{target,0,new Vector2(-100,-100),false,true});
            readback=RenderTexture.GetTemporary(width,height,0,RenderTextureFormat.ARGB32);
            Graphics.Blit(target,readback,new Vector2(1,-1),new Vector2(0,1));RenderTexture.active=readback;
            image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
            File.WriteAllBytes(path,image.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active=previous;if(image!=null)UnityEngine.Object.DestroyImmediate(image);
            if(readback!=null)RenderTexture.ReleaseTemporary(readback);target.Release();UnityEngine.Object.DestroyImmediate(target);
        }
    }
    static void Finish(string error)
    {
        if(steps==null)return;
        steps=null;EditorApplication.update-=Tick;SessionState.SetBool(Pending,false);previewRecording=false;
        if(recording){AudioRenderer.Stop();recording=false;}
        Time.captureDeltaTime=previousCaptureDelta;AudioSettings.Reset(previousConfiguration);
        AudioListener.volume=previousListenerVolume;AudioListener.pause=previousListenerPause;
        if(keyboard!=null)InputSystem.RemoveDevice(keyboard);
        if(originalInput!=null)InputSystem.settings=originalInput;
        if(testInput!=null)UnityEngine.Object.DestroyImmediate(testInput);
        Application.runInBackground=SessionState.GetBool(Pending+".background",false);
        if(SessionState.GetBool(Pending+".hadHigh",false))PlayerPrefs.SetInt("jni.highscore",SessionState.GetInt(Pending+".high",0));
        else PlayerPrefs.DeleteKey("jni.highscore");PlayerPrefs.Save();
        string previous=SessionState.GetString(Pending+".run","");
        if(previous.Length>0)JsonUtility.FromJsonOverwrite(previous,Resources.Load<RunState>("RunState"));
        Time.timeScale=1;
        File.AppendAllText(Output,error==null?"ALL MECH AUDIO CHECKS PASSED\n":"FAIL: "+error+"\n");
        if(error==null)Debug.Log("MECH AUDIO CAPTURE PASSED");else Debug.LogError(error);
        if(Application.isBatchMode)EditorApplication.Exit(error==null?0:1);else EditorApplication.isPlaying=false;
    }
}
