using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using JumpNotIncluded;

// Staged QA footage rendered by the actual game cameras/UI, with real cinematic time.
[InitializeOnLoad]
public static class ArrivalCapture
{
    const string Pending="jni.arrival.capture";
    static IEnumerator steps;
    static double deadline;
    static int lastFrame;
    static InputSettings originalInput,testInput;
    static SceneRoot Game=>UnityEngine.Object.FindFirstObjectByType<SceneRoot>();
    static string Output=>Path.GetFullPath("../work/arrival-ui-checks.txt");
    static string PreviewDirectory=>Path.GetFullPath("../artifacts/arrival-previews");
    static ArrivalCapture()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Pending,false))Begin();
            if(state==PlayModeStateChange.ExitingPlayMode&&steps!=null)Finish("Play mode stopped before arrival capture completed.");
        };
    }
    [MenuItem("Tools/Jump Not Included/Capture mech arrival")]
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
        Directory.CreateDirectory(Path.GetDirectoryName(Output));Directory.CreateDirectory(PreviewDirectory);
        Directory.CreateDirectory(Path.Combine(PreviewDirectory,"frames"));
        File.WriteAllText(Output,"STAGED ARRIVAL QA / actual game camera and real elapsed cinematic time\n");
        File.WriteAllText(Path.Combine(PreviewDirectory,"README.md"),
            "# War God arrival: staged QA captures\n\n"+
            "These captures use the real Unity game camera and cinematic. The helper stages sufficient wallet credit, purchases the replica and completes the prerequisite revival advertisement. It never changes the cinematic elapsed time or calls its completion method.\n\n"+
            "The sequence runs in real time. At selected stages the focus flag pauses it while the same stage is rendered at 1280x720, 960x540 and 1024x768. Video frames have actual cinematic timestamps in frames/timing.csv, allowing variable-frame-rate preview assembly without pretending that paused capture time was gameplay. No audio is recorded by this still capture helper.\n\n"+
            "The v1.14.0 sequence uses the redrawn 16 PPU pixel chassis and opaque pixel effects. A separate World 2 fixture starts from the x=80 checkpoint and captures consciousness transfer to the elevated hull at 3.75 seconds. Each image verifies that Mario's center and the upper corners of the actual visible hull bounds are inside the cinematic letterbox.\n\n"+
            "The helper restores the editor's run data, high score, input settings and background preference when complete.\n");
        File.WriteAllText(Path.Combine(PreviewDirectory,"frames/timing.csv"),"file,cinematic_seconds\n");
        if(EditorApplication.isPlaying)Begin();
        else{EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");EditorApplication.isPlaying=true;}
    }
    static void Begin()
    {
        if(steps!=null)return;
        Application.runInBackground=true;
        originalInput=InputSystem.settings;testInput=UnityEngine.Object.Instantiate(originalInput);
        testInput.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        testInput.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings=testInput;
        steps=Run();deadline=EditorApplication.timeSinceStartup+90;lastFrame=-1;EditorApplication.update+=Tick;
    }
    static double Wait(float seconds=.15f)=>EditorApplication.timeSinceStartup+seconds;
    static void Tick()
    {
        if(steps==null)return;
        try
        {
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Arrival capture timed out.");
            if(lastFrame==Time.frameCount){EditorApplication.QueuePlayerLoopUpdate();return;}
            lastFrame=Time.frameCount;
            if(steps.Current is double until&&EditorApplication.timeSinceStartup<until)return;
            if(!steps.MoveNext())Finish(null);
        }
        catch(Exception e){Finish(e.ToString());}
    }
    static void Check(bool condition,string label)
    {if(!condition)throw new Exception(label);File.AppendAllText(Output,"PASS: "+label+"\n");}
    static void Finish(string error)
    {
        if(steps==null)return;
        steps=null;EditorApplication.update-=Tick;SessionState.SetBool(Pending,false);
        if(originalInput!=null)InputSystem.settings=originalInput;
        if(testInput!=null)UnityEngine.Object.DestroyImmediate(testInput);
        Application.runInBackground=SessionState.GetBool(Pending+".background",false);
        if(SessionState.GetBool(Pending+".hadHigh",false))PlayerPrefs.SetInt("jni.highscore",SessionState.GetInt(Pending+".high",0));
        else PlayerPrefs.DeleteKey("jni.highscore");
        PlayerPrefs.Save();
        string previous=SessionState.GetString(Pending+".run","");
        if(previous.Length>0)JsonUtility.FromJsonOverwrite(previous,Resources.Load<RunState>("RunState"));
        Time.timeScale=1;
        File.AppendAllText(Output,error==null?"ALL ARRIVAL UI CHECKS PASSED\n":"FAIL: "+error+"\n");
        if(error==null)Debug.Log("ARRIVAL CAPTURE PASSED");else Debug.LogError(error);
        if(Application.isBatchMode)EditorApplication.Exit(error==null?0:1);
        else EditorApplication.isPlaying=false;
    }
    static IEnumerator Views(string name)
    {
        var g=Game;bool focus=g.hasFocus;g.hasFocus=false;
        Capture(name,1280,720,false);yield return Wait(.04f);Capture(name,1280,720);
        Capture(name+"-small",960,540);Capture(name+"-4x3",1024,768);
        Capture("warm-wide",1280,720,false);g.hasFocus=focus;
    }
    static IEnumerator Run()
    {
        Resources.Load<RunState>("RunState").NewRun();SceneManager.LoadScene("World01");yield return Wait(.2f);
        var g=Game;g.Run.Credit(RunModel.MechCost);g.SaveCheckpoint(g.session.checkpointPosition);
        g.KillPlayer("Arrival presentation fixture.");g.OpenShop();yield return Wait(.2f);g=Game;
        Check(g.BuyMech()&&!g.player.MechActive&&!g.Run.mechDeployed,
            "staged purchase orders the replica without equipping before arrival");
        g.Retry();g.hasFocus=true;g.AdvanceAd(SceneRoot.ReviveAdDuration);
        Check(g.mode==ScreenMode.Deployment&&!g.Playing&&g.mechArrival!=null,
            "the completed revival ad starts the non-interactive arrival sequence");
        float playTime=g.Run.playTime,controlTime=g.Run.activeInputTime,adTime=g.Run.adWatchTime;
        float[] times={.9f,1.65f,2.45f,3.45f,4.1f,5.05f};
        string[] names={"01-descent","02-braking-burn","03-landing","04-consciousness-light","05-neural-link","06-synchronization"};
        int nextStill=0,frame=0;float nextFrame=0;
        while(g.mode==ScreenMode.Deployment)
        {
            float time=g.mechArrival.Elapsed;
            if(time>=nextFrame)
            {
                string file="frames/frame_"+frame.ToString("D4")+".png";
                Capture(Path.ChangeExtension(file,null),1280,720);
                File.AppendAllText(Path.Combine(PreviewDirectory,"frames/timing.csv"),
                    Path.GetFileName(file)+","+time.ToString("F4",CultureInfo.InvariantCulture)+"\n");
                nextFrame=time+1f/10;frame++;
            }
            if(nextStill<times.Length&&time>=times[nextStill])
            {
                var views=Views(names[nextStill]);while(views.MoveNext())yield return views.Current;
                Check(g.mode==ScreenMode.Deployment&&!g.player.MechActive,
                    names[nextStill]+" remains mandatory and cannot grant control early");
                Check(!g.input.move.enabled&&!g.input.jump.enabled&&!g.input.fire.enabled,
                    names[nextStill]+" keeps gameplay input disabled throughout the cinematic");
                nextStill++;
            }
            yield return null;
        }
        Check(nextStill==times.Length&&frame>20,"real-time arrival renders all six stages and a usable preview frame sequence");
        Check(g.Playing&&g.Run.mechDeployed&&g.player.MechActive&&(g.mechArrival==null||g.mechArrival.Finished),
            "consciousness synchronization equips the suit and returns normal control");
        Check(g.Run.playTime-playTime<.15f&&g.Run.activeInputTime==controlTime&&g.Run.adWatchTime==adTime,
            "the arrival footage does not inflate gameplay, control or ad counters");
        var finalViews=Views("07-control-restored");while(finalViews.MoveNext())yield return finalViews.Current;
        Capture("frames/frame_"+frame.ToString("D4"),1280,720);
        File.AppendAllText(Path.Combine(PreviewDirectory,"frames/timing.csv"),
            "frame_"+frame.ToString("D4")+".png,"+MechArrival.Duration.ToString("F4",CultureInfo.InvariantCulture)+"\n");

        // World 2's tall brickwork raises the delivered hull. Use the real death/shop/revival
        // path from a saved small-Mario checkpoint to verify the two subjects remain framed.
        g.session.NewRun();SceneManager.LoadScene("World02");yield return Wait(.2f);g=Game;
        g.Run.Credit(RunModel.MechCost);g.SaveCheckpoint(new Vector2(80,.55f));
        g.KillPlayer("Elevated arrival presentation fixture.");g.OpenShop();yield return Wait(.2f);g=Game;
        Check(g.world==2&&Mathf.Abs(g.player.body.position.x-80)<.05f&&g.player.forms.Value==Form.Small,
            "elevated framing fixture restores small Mario at the World 2 x=80 checkpoint");
        Check(g.BuyMech()&&!g.Run.mechDeployed,"elevated fixture purchases a fresh, undeployed replica");
        g.Retry();g.hasFocus=true;g.AdvanceAd(SceneRoot.ReviveAdDuration);
        Check(g.mode==ScreenMode.Deployment&&g.mechArrival!=null&&g.mechArrival.LandingPosition.y>5,
            "World 2 brickwork raises the real delivery position above five world units");
        while(g.mode==ScreenMode.Deployment&&g.mechArrival.Elapsed<3.75f)yield return null;
        Check(g.mode==ScreenMode.Deployment&&g.mechArrival.Elapsed<4&&!g.Run.mechDeployed,
            "elevated fixture reaches the consciousness transfer using only real cinematic time");
        g.hasFocus=false;
        var elevatedViews=Views("08-elevated-uplink");while(elevatedViews.MoveNext())yield return elevatedViews.Current;
    }
    static void CheckElevatedFraming(string name)
    {
        var g=Game;
        var mario=g.cameraView.WorldToViewportPoint(g.player.visual.bounds.center);
        Bounds bounds=g.mechArrival.HullBounds;
        var hullLeft=g.cameraView.WorldToViewportPoint(new Vector3(bounds.min.x,bounds.max.y,bounds.center.z));
        var hullRight=g.cameraView.WorldToViewportPoint(new Vector3(bounds.max.x,bounds.max.y,bounds.center.z));
        bool Visible(Vector3 point)=>point.x>0&&point.x<1&&point.y>1f/6&&point.y<.8933f&&point.z>0;
        Check(bounds.size.x>0&&bounds.size.y>0&&Visible(mario)&&Visible(hullLeft)&&Visible(hullRight),
            name+" keeps Mario's center and both actual visible hull top corners inside the letterbox (Mario="+mario+", left="+hullLeft+", right="+hullRight+")");
    }
    static void Capture(string name,int width,int height,bool save=true)
    {
        var flags=BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.FlattenHierarchy;
        var render=typeof(EditorGUIUtility).GetMethod("RenderPlayModeViewCamerasInternal",flags);
        if(render==null)throw new MissingMethodException("Unity offscreen game renderer was not found.");
        Type viewType=null;
        foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {viewType=assembly.GetType("UnityEditor.PlayModeView");if(viewType!=null)break;}
        if(viewType==null)throw new MissingMemberException("PlayModeView type is missing.");
        var view=viewType.GetMethod("GetMainPlayModeView",flags).Invoke(null,null);
        if(view==null)
        {
            view=ScriptableObject.CreateInstance(viewType.Assembly.GetType("UnityEditor.GameView"));
            var parent=typeof(EditorWindow).GetField("m_Parent",BindingFlags.Instance|BindingFlags.NonPublic);
            parent.SetValue(view,ScriptableObject.CreateInstance(parent.FieldType));
        }
        viewType.GetProperty("targetSize",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).SetValue(view,new Vector2(width,height));
        Game.cameraView.pixelRect=new Rect(0,0,width,height);Game.cameraView.aspect=(float)width/height;
        var target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);target.Create();
        var before=RenderTexture.active;
        try
        {
            Event.current=new Event{type=EventType.Repaint};
            render.Invoke(null,new object[]{target,0,new Vector2(-100,-100),false,true});
            if(!save)return;
            var readback=RenderTexture.GetTemporary(width,height,0,RenderTextureFormat.ARGB32);
            Graphics.Blit(target,readback,new Vector2(1,-1),new Vector2(0,1));RenderTexture.active=readback;
            var image=new Texture2D(width,height,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
            File.WriteAllBytes(Path.Combine(PreviewDirectory,name+".png"),image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);RenderTexture.ReleaseTemporary(readback);
            if(!name.StartsWith("frames/",StringComparison.Ordinal))File.AppendAllText(Output,"CAPTURED: "+name+" ("+width+"x"+height+")\n");
            if(name.StartsWith("08-elevated-uplink",StringComparison.Ordinal))CheckElevatedFraming(name);
        }
        finally{RenderTexture.active=before;target.Release();UnityEngine.Object.DestroyImmediate(target);}
    }
}
