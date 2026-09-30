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
public static class PromotionCapture
{
    const string Pending="jni.promotion.capture";
    static IEnumerator steps;
    static double deadline;
    static int lastFrame;
    static Keyboard keyboard;
    static InputSettings originalInput,testInput;
    static SceneRoot Game=>UnityEngine.Object.FindFirstObjectByType<SceneRoot>();
    static string Output=>Path.GetFullPath("../work/promotion-ui-checks.txt");
    static string PreviewDirectory=>Path.GetFullPath("../artifacts/promotion-previews");
    static PromotionCapture()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool(Pending,false))Begin();
            if(state==PlayModeStateChange.ExitingPlayMode&&steps!=null)Finish("Play mode stopped before capture completed.");
        };
    }
    [MenuItem("Tools/Jump Not Included/Capture promotion UI")]
    public static void Start()
    {
        if(steps!=null)return;
        overflow.Clear();
        textSeen.Clear();
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
            "# v1.15.0 promotion UI captures\n\nActual Unity Play Mode renders, with staged balances, deaths and campaign selection. Buttons use the real Input System / IMGUI submit path. Screenshots are not concept art or a human playthrough. Captures cover 1280x720, 960x540, 1024x768 and 1920x1080. Run state, high score and input settings are restored.\n");
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
        keyboard=InputSystem.AddDevice<Keyboard>("JNI promotion UI keyboard");
        GameUI.ObserveText+=InspectText;
        steps=Run();deadline=EditorApplication.timeSinceStartup+150;lastFrame=-1;EditorApplication.update+=Tick;
    }
    static double Wait(float seconds=.2f)=>EditorApplication.timeSinceStartup+seconds;
    static void Tick()
    {
        if(steps==null)return;
        try
        {
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Promotion capture timed out.");
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
        GameUI.ObserveText-=InspectText;
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
        File.AppendAllText(Output,error==null?"ALL PROMOTION UI CHECKS PASSED\n":"FAIL: "+error+"\n");
        if(error==null)Debug.Log("PROMOTION CAPTURE PASSED");else Debug.LogError(error);
        if(Application.isBatchMode)EditorApplication.Exit(error==null?0:1);
        else EditorApplication.isPlaying=false;
    }
    static string renderName;
    static readonly System.Collections.Generic.HashSet<string> overflow=new System.Collections.Generic.HashSet<string>();
    static readonly System.Collections.Generic.HashSet<string> textSeen=new System.Collections.Generic.HashSet<string>();
    static void InspectText(Rect rect,string value,GUIStyle style)
    {
        if(Event.current.type!=EventType.Repaint||string.IsNullOrEmpty(value))return;
        textSeen.Add(value);
        float required=style.CalcHeight(new GUIContent(value),rect.width);
        if(required>rect.height+3)overflow.Add(renderName+": "+value.Replace("\n"," / ")+" needs "+required+" in "+rect.height);
        if(value.Contains("o x")||value.Contains("O x"))throw new Exception("Letter coin placeholder is still rendered: "+value);
        if(rect.x<0||rect.y<0||rect.xMax>1601||rect.yMax>901)throw new Exception("Text outside logical viewport: "+value);
    }
    static IEnumerator Views(string name)
    {
        Capture(name,1280,720,false);yield return Wait();Capture(name,1280,720);
        Capture(name+"-small",960,540);Capture(name+"-4x3",1024,768);
        Capture(name+"-1080p",1920,1080);
    }
    static void Submit(int index)
    {
        Capture("focus",1280,720,false);
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        typeof(GameUI).GetField("focus",flags).SetValue(Game.ui,index);
        // Queue an actual Enter press, let the UI read its InputAction, then render it.
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Enter));InputSystem.Update();
        typeof(GameUI).GetMethod("Update",flags).Invoke(Game.ui,null);
        Capture("submit",1280,720,false);
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();
    }
    static IEnumerator Run()
    {
        Resources.Load<RunState>("RunState").NewRun();
        var menuViews=Views("00-main-menu");while(menuViews.MoveNext())yield return menuViews.Current;
        SceneManager.LoadScene("World01");yield return Wait();
        var g=Game;g.hasFocus=false;Time.timeScale=0;
        Check(g.assets.Sprite("coin").name=="coin"&&g.assets.Sprite("coin").rect.width==10,"UI resolves the actual 10x14 coin sprite");
        var views=Views("01-gameplay-coins");while(views.MoveNext())yield return views.Current;
        g.Run.coins=1000;g.session.Capture(g.session.checkpointPosition,g.session.checkpointForm);
        views=Views("02-hud-1000");while(views.MoveNext())yield return views.Current;
        Check(textSeen.Contains("x00")&&textSeen.Contains("x1000"),"HUD renders 00 and 1000 unchanged as numeric text beside the coin sprite");
        g.KillPlayer("A Goomba has ended your free trial.");
        views=Views("03-comeback-offer");while(views.MoveNext())yield return views.Current;
        g.ui.ResetFocus();
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.DownArrow));InputSystem.Update();
        typeof(GameUI).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(g.ui,null);
        Check((int)typeof(GameUI).GetField("focus",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(g.ui)==1,
            "Down-arrow navigation moves from revival to the cash-ad button");
        InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.Update();
        Submit(1);g=Game;g.hasFocus=false;
        Check(g.mode==ScreenMode.Ad&&g.adKind==AdKind.Cash,"Death-page earn button opens a cash ad");
        int wallet=g.Run.wallet;g.hasFocus=true;g.AdvanceAd(1.25f);g.hasFocus=false;
        Check(g.Run.wallet==wallet+100&&g.adEarned==100,"One full ad second credits exactly S$1; fractional second adds nothing");
        foreach(AdCampaign campaign in Enum.GetValues(typeof(AdCampaign)))
        {
            g.adCampaign=campaign;views=Views("04-cash-"+campaign);while(views.MoveNext())yield return views.Current;
        }
        float before=g.adTime;g.AdvanceAd(10);
        Check(g.adTime==before&&g.Run.wallet==wallet+100,"Unfocused ad cannot advance time or rewards");
        Submit(0);yield return Wait();g=Game;g.hasFocus=false;
        Check(g.mode==ScreenMode.Shop&&g.Run.wallet==wallet+100&&g.ReviveRequired,"Collect button keeps banked cash and preserves revival requirement");
        views=Views("05-upgrade-shop");while(views.MoveNext())yield return views.Current;
        Submit(3);
        Check(g.Run.Owns(Product.Jump)&&g.Run.coins==801,"Coin-price button buys Jump for exactly 199 coins");
        int balance=g.Run.coins;Submit(3);
        Check(g.Run.coins==balance,"Owned upgrade button cannot charge twice");
        Submit(7);
        Check(!g.Run.Owns(Product.DoubleJump)&&g.Run.coins==balance,"Insufficient coin balance keeps the purchase disabled");
        g.toastUntil=0;Submit(0);
        Check(g.rechargeOpen&&g.checkoutStep==CheckoutStep.Packs,"Coin-balance plus button opens the pack chooser");
        views=Views("06-coin-packs");while(views.MoveNext())yield return views.Current;
        Submit(1);Check(g.selectedPack==0,"First pack selects 100 coins for S$1.00");
        Submit(3);Check(g.selectedPack==2,"Third pack selects 2000 coins for S$18.80");
        Submit(2);Check(g.selectedPack==1,"Middle pack selects 1000 coins for S$9.80");
        Submit(7);Check(g.checkoutStep==CheckoutStep.LinkCard,"Continue opens virtual-card linking");
        Submit(1);Check(g.checkoutStep==CheckoutStep.Review&&g.Run.Payments.cardLinked,"Link button reaches review without a charge");
        views=Views("07-checkout-review");while(views.MoveNext())yield return views.Current;
        Submit(1);Check(g.checkoutStep==CheckoutStep.Processing,"Pay button begins confirmation");
        Submit(2);Check(g.checkoutStep==CheckoutStep.Packs&&g.Run.coins==balance&&g.Run.Payments.orders.Count==0,"Cancel payment leaves coins and charges untouched");
        Submit(7);Submit(1);g.hasFocus=true;g.AdvancePayment(SceneRoot.PaymentDuration);g.hasFocus=false;
        Check(g.checkoutStep==CheckoutStep.Receipt&&g.Run.coins==balance+1000&&g.Run.Payments.orders.Count==1,"Confirmed payment grants precisely 1000 coins once");
        views=Views("08-coin-receipt");while(views.MoveNext())yield return views.Current;
        Submit(3);Check(g.checkoutStep==CheckoutStep.Activity,"Receipt opens payment activity");
        views=Views("09-payment-activity");while(views.MoveNext())yield return views.Current;
        Submit(1);Check(g.checkoutStep==CheckoutStep.Receipt,"Activity receipt button reopens the original order");
        Submit(1);Check(!g.rechargeOpen&&g.mode==ScreenMode.Shop,"Receipt back button returns to upgrades");
        g.toastUntil=0;Submit(1);Check(g.mode==ScreenMode.Dead,"Shop Back returns to the mandatory death screen");
        Submit(0);Check(g.mode==ScreenMode.Ad&&g.adKind==AdKind.Revive,"Primary comeback button starts a revival ad");
        g.hasFocus=false;
        foreach(AdCampaign campaign in Enum.GetValues(typeof(AdCampaign)))
        {g.adCampaign=campaign;views=Views("10-revive-"+campaign);while(views.MoveNext())yield return views.Current;}
        Capture("revive-controls",1280,720,false);
        Check((int)typeof(GameUI).GetField("lastCount",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(g.ui)==0,"Revival ad exposes no skip or collect button");
        g.hasFocus=true;g.AdvanceAd(1.99f);g.hasFocus=false;
        Check(g.mode==ScreenMode.Ad&&g.ReviveRequired,"Revival still requires the full two seconds");
        g.hasFocus=true;g.AdvanceAd(.02f);yield return Wait();g=Game;g.hasFocus=false;Time.timeScale=0;
        Check(g.Playing&&!g.ReviveRequired&&g.Run.Owns(Product.Jump),"Completed revival returns to gameplay with purchased upgrade");
        g.CompleteWorld();views=Views("11-results-coins");while(views.MoveNext())yield return views.Current;
        g.Run.wallet=RunModel.WalletLimit;g.session.Capture(g.session.checkpointPosition,g.session.checkpointForm);
        g.SetMode(ScreenMode.Playing);g.KillPlayer("The wallet is full. The offer is still irresistible.");g.hasFocus=false;
        Submit(1);Check(g.mode==ScreenMode.Dead&&g.Run.wallet==9900,"Full-wallet earn button stays disabled at S$99.00");
        views=Views("12-wallet-full");while(views.MoveNext())yield return views.Current;
        g.Run.wallet=9850;g.session.Capture(g.session.checkpointPosition,g.session.checkpointForm);Submit(1);g.hasFocus=true;g.AdvanceAd(1);yield return Wait();g=Game;g.hasFocus=false;
        Check(g.Run.wallet==9900&&g.mode==ScreenMode.Shop,"Cash ad stops automatically at the S$99 wallet cap");
        g.toastUntil=0;g.Run.coins=int.MaxValue;
        views=Views("13-large-balance");while(views.MoveNext())yield return views.Current;
        g.Run.coins=1801;g.OpenMechShowcase();
        views=Views("14-mech-showcase");while(views.MoveNext())yield return views.Current;
        File.WriteAllLines(Path.GetFullPath("../work/promotion-text-overflow.txt"),overflow);
        Check(overflow.Count==0,"All observed text fits its allocated layout rectangle at four window sizes");
        Check(g.Run.Payments.orders.Count==1,"Presentation interactions preserve the payment ledger");
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
            renderName=name;
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
