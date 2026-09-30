using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using JumpNotIncluded;

// Staged presentation fixtures rendered from the real game view, never the desktop.
[InitializeOnLoad]
public static class MechCapture
{
    const string Pending="jni.mech.capture";
    static IEnumerator steps;
    static double deadline;
    static int lastFrame;
    static Keyboard keyboard;
    static InputSettings originalInput,testInput;
    static SceneRoot Game=>UnityEngine.Object.FindFirstObjectByType<SceneRoot>();
    static string Output=>Path.GetFullPath("../work/mech-ui-checks.txt");
    static string PreviewDirectory=>Path.GetFullPath("../artifacts/mech-previews");
    static MechCapture()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Pending,false))Begin();
            if(state==PlayModeStateChange.ExitingPlayMode&&steps!=null)Finish("Play mode stopped before capture completed.");
        };
    }
    [MenuItem("Tools/Jump Not Included/Capture mech presentation")]
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
        Directory.CreateDirectory(Path.GetDirectoryName(Output));
        File.WriteAllText(Output,"STAGED PRESENTATION FIXTURES / real UI and Input System\n");
        Directory.CreateDirectory(PreviewDirectory);
        File.WriteAllText(Path.Combine(PreviewDirectory,"README.md"),
            "# War God presentation captures\n\n"+
            "These are staged QA fixtures, not a recorded human playthrough. Wallet credit, world selection and initial player position are set by the editor capture helper. The shop and purchase buttons use the real game UI. Flight and the laser are driven through the real Input System keyboard controls. The v1.14.0 playable SL CHEATER crossover uses six redrawn pixel poses on Mario's effective 16 PPU grid, independent pixel exhaust and a pixel-rendered auto-lock laser.\n\n"+
            "Shop/showcase views are rendered at 1280x720, 960x540 and 1024x768. Flight and laser stills are rendered at 1280x720. The helper preserves the editor high score, run data, input settings and background preference.\n");
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
        keyboard=InputSystem.AddDevice<Keyboard>("JNI mech presentation keyboard");
        steps=Run();deadline=EditorApplication.timeSinceStartup+90;lastFrame=-1;EditorApplication.update+=Tick;
    }
    static double Wait(float seconds=.2f)=>EditorApplication.timeSinceStartup+seconds;
    static void Tick()
    {
        if(steps==null)return;
        try
        {
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Mech capture timed out.");
            if(lastFrame==Time.frameCount){EditorApplication.QueuePlayerLoopUpdate();return;}
            lastFrame=Time.frameCount;
            if(steps.Current is double until&&EditorApplication.timeSinceStartup<until)return;
            if(!steps.MoveNext())Finish(null);
        }
        catch(Exception e){Finish(e.ToString());}
    }
    static void Check(bool condition,string label)
    {if(!condition)throw new Exception(label);File.AppendAllText(Output,"PASS: "+label+"\n");}
    static void Keys(params Key[] keys)=>InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));
    static void Finish(string error)
    {
        if(steps==null)return;
        steps=null;EditorApplication.update-=Tick;SessionState.SetBool(Pending,false);
        if(keyboard!=null){InputSystem.RemoveDevice(keyboard);keyboard=null;}
        if(originalInput!=null)InputSystem.settings=originalInput;
        if(testInput!=null)UnityEngine.Object.DestroyImmediate(testInput);
        Application.runInBackground=SessionState.GetBool(Pending+".background",false);
        if(SessionState.GetBool(Pending+".hadHigh",false))PlayerPrefs.SetInt("jni.highscore",SessionState.GetInt(Pending+".high",0));
        else PlayerPrefs.DeleteKey("jni.highscore");
        PlayerPrefs.Save();
        string previous=SessionState.GetString(Pending+".run","");
        if(previous.Length>0)JsonUtility.FromJsonOverwrite(previous,Resources.Load<RunState>("RunState"));
        Time.timeScale=1;
        File.AppendAllText(Output,error==null?"ALL MECH UI CHECKS PASSED\n":"FAIL: "+error+"\n");
        if(error==null)Debug.Log("MECH CAPTURE PASSED");else Debug.LogError(error);
        if(Application.isBatchMode)EditorApplication.Exit(error==null?0:1);
        else EditorApplication.isPlaying=false;
    }
    static IEnumerator Views(string name)
    {
        Capture(name,1280,720,false);yield return Wait();Capture(name,1280,720);
        Capture(name+"-small",960,540);Capture(name+"-4x3",1024,768);
    }
    static void SubmitMechButton()
    {
        Capture("focus",1280,720,false);
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        var ui=Game.ui;
        Check(Game.mechShowcaseOpen&&(int)typeof(GameUI).GetField("lastCount",flags).GetValue(ui)==5,
            "Mech showcase exposes the expected five navigation targets");
        typeof(GameUI).GetField("focus",flags).SetValue(ui,4);
        typeof(GameUI).GetField("submit",flags).SetValue(ui,true);
        Capture("submit",1280,720,false);
    }
    static IEnumerator Run()
    {
        Resources.Load<RunState>("RunState").NewRun();SceneManager.LoadScene("World01");yield return Wait();
        var g=Game;g.KillPlayer("Presentation fixture: a defeat unlocks the upgrade shop.");g.OpenShop();yield return Wait();
        g=Game;g.hasFocus=false;
        var views=Views("01-upgrade-shop");while(views.MoveNext())yield return views.Current;
        g.OpenMechShowcase();views=Views("02-mech-insufficient");while(views.MoveNext())yield return views.Current;
        SubmitMechButton();
        Check(!g.Run.Owns(Product.Mech)&&g.Run.wallet==0&&g.mode==ScreenMode.Shop,
            "Disabled insufficient-funds button leaves ownership and balances unchanged");
        g.Run.Credit(9000);g.session.Capture(g.session.checkpointPosition,g.session.checkpointForm);
        views=Views("03-mech-affordable");while(views.MoveNext())yield return views.Current;
        int coins=g.Run.coins;SubmitMechButton();
        Check(g.Run.Owns(Product.Mech)&&g.Run.wallet==1001&&g.Run.coins==coins&&g.Run.WalletPaidTotal()==7999,
            "Rendered buy button installs the replica and charges SGD 79.99 without spending coins");
        Check(g.ReviveRequired&&g.mode==ScreenMode.Shop&&!g.Playing,
            "Mech purchase preserves the mandatory revival gate");
        Check(!g.BuyMech()&&g.Run.wallet==1001&&g.Run.WalletPaidTotal()==7999,
            "Repeated purchase is rejected without a second charge");
        g.toastUntil=0;
        views=Views("04-mech-owned");while(views.MoveNext())yield return views.Current;
        SubmitMechButton();
        Check(g.mode==ScreenMode.Ad&&g.adKind==AdKind.Revive&&g.ReviveRequired&&g.Run.wallet==1001,
            "Owned deploy button starts the required revival ad without charging again");
        g.hasFocus=true;g.AdvanceAd(SceneRoot.ReviveAdDuration);
        Check(g.Deploying&&!g.ReviveRequired&&!g.Run.mechDeployed,
            "Completing the revival ad starts the mandatory orbital arrival before equipment activation");
        double deploymentDeadline=EditorApplication.timeSinceStartup+MechArrival.Duration+5;
        while(g.Deploying&&EditorApplication.timeSinceStartup<deploymentDeadline)yield return Wait(.05f);
        Check(g.Playing&&!g.ReviveRequired&&g.Run.Owns(Product.Mech),
            "Completing the full arrival deploys the purchased replica");
        g.session.checkpointJson="";g.session.restoreCheckpoint=false;g.session.carryForm=Form.Small;
        SceneManager.LoadScene("World02");yield return Wait();g=Game;g.hasFocus=true;
        var p=g.player;p.body.position=new Vector2(4,3);p.body.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();
        g.cameraView.transform.position=new Vector3(11,4.7f,-10);
        Capture("warm-flight",1280,720,false);
        Keys(Key.Space);yield return Wait(.34f);
        Check(p.mech.IsThrusting&&p.body.position.y>4.2f,"Real SPACE input engages animated ascent beside World 2 enemies");
        Capture("05-mech-flight",1280,720);
        Keys();yield return Wait(.08f);float altitude=p.body.position.y;yield return Wait(.12f);
        Check(Mathf.Abs(p.body.position.y-altitude)<.08f,"Releasing SPACE holds the flight altitude");
        int shots=p.mech.ShotsFired;Keys(Key.J);
        double until=EditorApplication.timeSinceStartup+2;
        while(p.mech.ShotsFired==shots&&EditorApplication.timeSinceStartup<until)yield return Wait(.01f);
        Check(p.mech.ShotsFired>shots&&!string.IsNullOrEmpty(p.mech.LastTarget),
            "Real J input fires an automatic laser at a live World 2 enemy");
        Check(UnityEngine.Object.FindFirstObjectByType<MechPixelLaser>()!=null,
            "The real J shot creates the pixel-rendered auto-lock laser");
        Capture("06-mech-auto-lock-laser",1280,720);Keys();
        Check(g.Playing&&g.Run.deaths==1,"Presentation flight and laser do not introduce a new death");
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
            File.AppendAllText(Output,"CAPTURED: "+name+" ("+width+"x"+height+")\n");
        }
        finally{RenderTexture.active=before;target.Release();UnityEngine.Object.DestroyImmediate(target);}
    }
}
