using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using JumpNotIncluded;

// Isolated propulsion QA: real terrain, real keyboard input and real game rendering.
[InitializeOnLoad]
public static class PropulsionCapture
{
    const string Pending="jni.propulsion.capture";
    static IEnumerator steps;
    static double deadline;
    static int lastFrame;
    static Keyboard keyboard;
    static InputSettings originalInput,testInput;
    static SceneRoot Game=>UnityEngine.Object.FindFirstObjectByType<SceneRoot>();
    static string Output=>Path.GetFullPath("../work/propulsion-checks.txt");
    static string PreviewDirectory=>Path.GetFullPath("../artifacts/propulsion-previews");

    static PropulsionCapture()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Pending,false))Begin();
            if(state==PlayModeStateChange.ExitingPlayMode&&steps!=null)Finish("Play mode stopped before propulsion checks completed.");
        };
    }
    [MenuItem("Tools/Jump Not Included/Capture mech propulsion")]
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
        Directory.CreateDirectory(PreviewDirectory);
        File.WriteAllText(Output,"STAGED PROPULSION QA / real terrain, Input System controls and game camera\n");
        File.WriteAllText(Path.Combine(PreviewDirectory,"README.md"),
            "# War God propulsion checks\n\n"+
            "These are staged Unity QA fixtures, not a human playthrough. The helper grants an already purchased and deployed suit to isolate propulsion from the independently checked arrival sequence. It removes interfering enemies, preserves the original ground, pipes and gaps, and repositions the initial ground, pipe and abyss fixtures.\n\n"+
            "Walking, ascent, hover, cruise in both directions and landing use the real Input System keyboard. The images are rendered by the actual game camera at 1280x720. The v1.14.0 chassis is redrawn as six pixel poses on Mario's effective 16 PPU grid; both exhaust directions use opaque three-color pixel geometry. Assertions inspect rendered resources, state, frame, exhaust direction and engine audio. The final comparison adds the actual big-idle Mario sprite at its unchanged 16 PPU scale beside the grounded mech, without adding any gameplay actor or collider. The helper restores editor run data, high score, Input System settings and background preference after execution.\n");
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
        keyboard=InputSystem.AddDevice<Keyboard>("JNI propulsion QA keyboard");
        steps=Run();deadline=EditorApplication.timeSinceStartup+75;lastFrame=-1;
        EditorApplication.update+=Tick;
    }
    static double Wait(float seconds=.2f)=>EditorApplication.timeSinceStartup+seconds;
    static void Tick()
    {
        if(steps==null)return;
        try
        {
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Propulsion checks timed out.");
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
    static void Position(PlayerMotor player,Vector2 position)
    {
        player.body.position=position;player.body.linearVelocity=Vector2.zero;
        Physics2D.SyncTransforms();
        Game.cameraView.transform.position=new Vector3(Mathf.Clamp(position.x+4,11,Game.level.length-10),4.7f,-10);
    }
    static void CheckGroundSilent(MechSuit suit,string phase,bool walking=false)
    {
        Check(suit.IsGrounded&&!suit.IsThrusting&&!suit.IsCruising,phase+" stays grounded without flight or cruise");
        Check(suit.Exhaust!=null&&!suit.Exhaust.Emitting&&suit.CruiseExhaust!=null&&!suit.CruiseExhaust.Emitting,
            phase+" emits neither vertical nor horizontal exhaust");
        Check(suit.ThrusterSource!=null&&!suit.ThrusterSource.isPlaying,phase+" stops the engine loop");
        Check(walking?suit.FlightFrame==1||suit.FlightFrame==2:suit.FlightFrame==0,
            phase+" uses the redrawn idle pose 0 or walking poses 1 and 2 without flight trails");
    }
    static void CheckVerticalFlight(MechSuit suit,string phase)
    {
        Check(!suit.IsGrounded&&!suit.IsCruising&&suit.FlightFrame==3,
            phase+" uses redrawn expanded-wing hover pose 3");
        Check(suit.Exhaust!=null&&suit.Exhaust.Emitting&&suit.Exhaust.Power>0&&
              suit.Exhaust.Direction.y<-.7f&&Mathf.Abs(suit.Exhaust.Direction.x)<.25f,
            phase+" uses active independent exhaust pointing downward");
        Check(suit.CruiseExhaust!=null&&!suit.CruiseExhaust.Emitting,
            phase+" disables the horizontal cruising trail");
        Check(suit.ThrusterSource!=null&&suit.ThrusterSource.isPlaying,phase+" plays the flight engine loop");
    }
    static void CheckCruise(PlayerMotor player,int facing,string phase)
    {
        var suit=player.mech;
        Check(!suit.IsGrounded&&suit.IsCruising&&(suit.FlightFrame==4||suit.FlightFrame==5),
            phase+" uses redrawn cruising poses 4 and 5");
        Check(player.facing==facing&&player.body.linearVelocity.x*facing>.5f,
            phase+" faces and travels in the commanded direction");
        Check(suit.CruiseExhaust!=null&&suit.CruiseExhaust.Emitting&&
              suit.CruiseExhaust.Direction.x*facing<-.7f&&Mathf.Abs(suit.CruiseExhaust.Direction.y)<.25f,
            phase+" emits an active pixel trail opposite the direction of travel");
    }
    static void CheckPixelResources(SceneRoot game,MechSuit suit)
    {
        var assets=game.assets;
        Check(assets.mechPixelAtlas!=null&&assets.mechPixelMaterial!=null&&
              assets.mechPixelMaterial.shader!=null&&assets.mechPixelMaterial.shader.isSupported,
            "the redrawn chassis atlas and pixel material resolve to a supported shader on this renderer");
        SpriteRenderer hull=null;
        foreach(var renderer in suit.GetComponentsInChildren<SpriteRenderer>())
            if(renderer.sprite!=null&&renderer.sprite.texture==assets.mechPixelAtlas){hull=renderer;break;}
        Check(hull!=null&&hull.sharedMaterial==assets.mechPixelMaterial,
            "the visible chassis actually uses the redrawn atlas and the supported pixel material");
        float cellWidth=hull.sprite.rect.width/hull.sprite.pixelsPerUnit/MechArt.Grid*Mathf.Abs(hull.transform.lossyScale.x);
        float cellHeight=hull.sprite.rect.height/hull.sprite.pixelsPerUnit/MechArt.Grid*Mathf.Abs(hull.transform.lossyScale.y);
        Check(Mathf.Abs(cellWidth-1f/16)<.0001f&&Mathf.Abs(cellHeight-1f/16)<.0001f&&
              Mathf.Abs(MechExhaust.PixelSize-1f/16)<.0001f,
            "the rendered chassis logical cells and exhaust pixels both have Mario's effective 16 PPU scale");
    }
    static void CheckRenderedPixelExhaust(MechSuit suit)
    {
        var palette=new HashSet<Color32>();bool opaque=true,pixelSized=true;int pixels=0;
        foreach(var filter in suit.GetComponentsInChildren<MeshFilter>())
        {
            var renderer=filter.GetComponent<MeshRenderer>();
            if(renderer==null||!renderer.enabled||filter.sharedMesh==null)continue;
            var mesh=filter.sharedMesh;var vertices=mesh.vertices;var colors=mesh.colors32;
            if(vertices.Length==0||colors.Length!=vertices.Length)continue;
            foreach(var color in colors){palette.Add(color);opaque&=color.a==255;}
            for(int i=0;i+3<vertices.Length;i+=4)
            {
                Vector3 a=filter.transform.TransformPoint(vertices[i]);
                Vector3 b=filter.transform.TransformPoint(vertices[i+1]);
                Vector3 c=filter.transform.TransformPoint(vertices[i+3]);
                pixelSized&=Mathf.Abs(Vector3.Distance(a,b)-1f/16)<.0001f&&
                            Mathf.Abs(Vector3.Distance(a,c)-1f/16)<.0001f;
                pixels++;
            }
        }
        Check(pixels>0&&pixelSized&&opaque&&palette.Count==3,
            "the live hover exhaust renders opaque one-sixteenth-unit squares in exactly three colors");
    }
    static IEnumerator Run()
    {
        Resources.Load<RunState>("RunState").NewRun();SceneManager.LoadScene("World01");yield return Wait();
        var g=Game;g.hasFocus=true;
        foreach(var enemy in UnityEngine.Object.FindObjectsByType<EnemyActor>(FindObjectsSortMode.None))
            UnityEngine.Object.Destroy(enemy.gameObject);
        foreach(var boss in UnityEngine.Object.FindObjectsByType<BossActor>(FindObjectsSortMode.None))
            UnityEngine.Object.Destroy(boss.gameObject);
        g.Run.Credit(RunModel.MechCost);
        Check(g.Run.BuyMech(),"fixture grants a paid replica independently of the arrival checks");
        g.Run.mechDeployed=true;
        var p=g.player;p.RefreshEquipment();Position(p,new Vector2(2,1.1f));Keys();yield return Wait(.35f);
        var suit=p.mech;int deaths=g.Run.deaths;
        Check(g.Playing&&p.MechActive&&!g.Deploying,"already deployed fixture starts normal control without changing the arrival gate");
        Check(Mathf.Abs(p.box.bounds.min.y)<.12f,"ground fixture stands on the original ground surface");
        CheckPixelResources(g,suit);
        CheckGroundSilent(suit,"Ground idle");Capture("01-ground-idle");

        float x=p.body.position.x;Keys(Key.D);yield return Wait(.3f);
        Check(p.body.position.x>x+.5f,"real D input walks the suit along the ground");
        CheckGroundSilent(suit,"Ground walk",true);Capture("02-ground-walk");
        Keys();yield return Wait(.12f);

        float y=p.body.position.y;Keys(Key.Space);yield return Wait(.6f);
        Check(suit.IsThrusting&&p.body.position.y>y+2,"real Space input lifts the suit above the ground");
        CheckVerticalFlight(suit,"Vertical lift");Capture("03-lift");

        Keys();yield return Wait(.2f);y=p.body.position.y;yield return Wait(.2f);
        Check(Mathf.Abs(p.body.position.y-y)<.08f&&!suit.IsThrusting,"releasing Space maintains altitude without continuing ascent");
        CheckVerticalFlight(suit,"Stationary hover");CheckRenderedPixelExhaust(suit);Capture("04-hover");

        Keys(Key.D);yield return Wait(.3f);CheckCruise(p,1,"Right cruise");Capture("05-cruise-right");
        Keys(Key.A);yield return Wait(.3f);CheckCruise(p,-1,"Left cruise");Capture("06-cruise-left");
        Keys();yield return Wait(.15f);
        CheckVerticalFlight(suit,"Hover after cruising");

        Keys(Key.S);double landingDeadline=EditorApplication.timeSinceStartup+3;
        while(!suit.IsGrounded&&EditorApplication.timeSinceStartup<landingDeadline)yield return Wait(.04f);
        Keys();yield return Wait(.18f);
        Check(Mathf.Abs(p.box.bounds.min.y)<.12f&&Mathf.Abs(p.body.linearVelocity.y)<.08f,
            "real S input lands on ground instead of hovering at the abyss safety altitude");
        CheckGroundSilent(suit,"Landed on ground");Capture("07-landed");

        Collider2D pipe=null;
        foreach(var collider in UnityEngine.Object.FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None))
            if(collider.gameObject.name=="Pipe"&&Mathf.Abs(collider.bounds.center.x-18)<.1f){pipe=collider;break;}
        Check(pipe!=null,"the original World 1 pipe remains in the fixture");
        float pipeTop=pipe.bounds.max.y;
        Position(p,new Vector2(pipe.bounds.center.x,pipeTop+MechSuit.HullHeight*.5f+2));
        Keys();yield return Wait(.15f);Keys(Key.S);landingDeadline=EditorApplication.timeSinceStartup+2;
        while(!suit.IsGrounded&&EditorApplication.timeSinceStartup<landingDeadline)yield return Wait(.04f);
        Keys();yield return Wait(.18f);
        Check(Mathf.Abs(p.box.bounds.min.y-pipeTop)<.12f&&pipe.enabled,
            "real S input lands on the original pipe top without removing terrain");
        CheckGroundSilent(suit,"Landed on pipe");Capture("08-pipe-landed");

        Position(p,new Vector2((g.level.GapStart+g.level.GapEnd)*.5f,-.5f));
        Keys(Key.S);yield return Wait(.35f);Keys();yield return Wait(.2f);y=p.body.position.y;yield return Wait(.15f);
        Check(g.Playing&&g.Run.deaths==deaths&&!suit.IsGrounded&&p.body.position.y>=MechSuit.MinAltitude-.03f,
            "the original abyss still activates hover protection without a new death");
        Check(Mathf.Abs(p.body.position.y-y)<.08f&&Mathf.Abs(p.body.linearVelocity.y)<.08f,
            "abyss protection holds altitude after descent is released");
        Check(Physics2D.Raycast(new Vector2(p.body.position.x,0),Vector2.down,2,1<<8).collider==null,
            "abyss protection leaves the original gap empty");
        CheckVerticalFlight(suit,"Abyss hover");Capture("09-abyss-hover");Keys();

        // Purely visual comparison: the real course sprite at its real scale,
        // on the same floor as the suit. No second player, collider or rules change.
        Position(p,new Vector2(8,1.1f));yield return Wait(.2f);
        CheckGroundSilent(suit,"Scale comparison ground idle");
        var marioObject=new GameObject("QA reference / original big Mario at 16 PPU");
        var mario=marioObject.AddComponent<SpriteRenderer>();mario.sprite=g.assets.Sprite("big-idle");
        mario.sharedMaterial=g.assets.blueKey;mario.sortingOrder=12;
        marioObject.transform.localScale=Vector3.one;
        marioObject.transform.position=new Vector3(p.body.position.x+2,-mario.sprite.bounds.min.y,0);
        yield return Wait(.08f);
        Check(mario.sprite.name=="big-idle"&&Mathf.Abs(mario.sprite.pixelsPerUnit-16)<.0001f&&
              Mathf.Abs(mario.bounds.min.y)<.04f&&Mathf.Abs(suit.HullBounds.min.y)<.08f,
            "the comparison places the actual 16 PPU big Mario and grounded chassis on the same floor");
        Capture("10-mario-scale");UnityEngine.Object.Destroy(marioObject);
    }
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
        File.AppendAllText(Output,error==null?"ALL PROPULSION CHECKS PASSED\n":"FAIL: "+error+"\n");
        if(error==null)Debug.Log("PROPULSION CAPTURE PASSED");else Debug.LogError(error);
        if(Application.isBatchMode)EditorApplication.Exit(error==null?0:1);
        else EditorApplication.isPlaying=false;
    }
    static void Capture(string name)
    {
        const int width=1280,height=720;
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
