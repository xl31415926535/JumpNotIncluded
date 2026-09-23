using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using JumpNotIncluded;

// Renders only the isolated Unity game view; does not capture the desktop.
[InitializeOnLoad]
public static class PaymentCapture
{
    static IEnumerator steps;
    static double deadline;
    static SceneRoot Game=>UnityEngine.Object.FindFirstObjectByType<SceneRoot>();
    static PaymentCapture()
    {
        EditorApplication.playModeStateChanged+=s=>
        {
            if(s==PlayModeStateChange.EnteredPlayMode&&SessionState.GetBool("jni.payment.capture",false))
            {steps=Run();deadline=EditorApplication.timeSinceStartup+70;EditorApplication.update+=Tick;}
        };
    }
    public static void Start()
    {
        JumpNotIncluded.EditorTools.ProjectSetup.Setup();
        SessionState.SetBool("jni.payment.capture",true);
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        EditorApplication.isPlaying=true;
    }
    static double Wait()=>EditorApplication.timeSinceStartup+.3;
    static void Tick()
    {
        try
        {
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Payment capture timed out.");
            if(steps.Current is double until&&EditorApplication.timeSinceStartup<until)return;
            if(!steps.MoveNext()){SessionState.SetBool("jni.payment.capture",false);EditorApplication.update-=Tick;Debug.Log("PAYMENT CAPTURE PASSED");EditorApplication.Exit(0);}
        }
        catch(Exception e){SessionState.SetBool("jni.payment.capture",false);Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static IEnumerator Views(string name)
    {
        Capture(name,1280,720,false);yield return Wait();Capture(name,1280,720);
        Capture(name+"-small",960,540);Capture(name+"-4x3",1024,768);
    }
    static void SubmitDefault(CheckoutStep expected)
    {
        // Exercise the same focused-button dispatch used by Enter, in the rendered IMGUI.
        Capture("focus",1280,720,false);
        var ui=UnityEngine.Object.FindFirstObjectByType<GameUI>();
        typeof(GameUI).GetField("submit",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(ui,true);
        Capture("submit",1280,720,false);
        if(Game.checkoutStep!=expected)throw new Exception("Wrong focused checkout action: "+Game.checkoutStep+", expected "+expected);
        Debug.Log("PAYMENT FOCUS PASSED: "+expected);
    }
    static IEnumerator Run()
    {
        Resources.Load<RunState>("RunState").NewRun();SceneManager.LoadScene("World01");yield return Wait();
        var g=Game;g.KillPlayer("Checkout preview.");g.OpenShop();yield return Wait();g=Game;g.OpenRecharge();g.hasFocus=false;
        var images=Views("sgd-packs");while(images.MoveNext())yield return images.Current;
        g.ShowPaymentActivity();images=Views("sgd-empty-activity");while(images.MoveNext())yield return images.Current;g.BackToPacks();
        SubmitDefault(CheckoutStep.LinkCard);images=Views("sgd-link-card");while(images.MoveNext())yield return images.Current;
        SubmitDefault(CheckoutStep.Review);images=Views("sgd-review");while(images.MoveNext())yield return images.Current;
        SubmitDefault(CheckoutStep.Processing);g.hasFocus=true;g.AdvancePayment(.7f);g.hasFocus=false;
        images=Views("sgd-processing");while(images.MoveNext())yield return images.Current;
        SubmitDefault(CheckoutStep.Packs);
        if(g.Run.Payments.orders.Count!=0||g.Run.coins!=0||g.Run.Payments.cardCharged!=0)throw new Exception("Focused cancellation charged the card.");
        g.ContinueCheckout();g.ConfirmTopUp();
        g.hasFocus=true;g.AdvancePayment(SceneRoot.PaymentDuration);g.hasFocus=false;
        images=Views("sgd-receipt");while(images.MoveNext())yield return images.Current;
        g.BackToPacks();g.SelectPayment(PaymentMethod.Wallet);g.ContinueCheckout();g.ConfirmTopUp();
        images=Views("sgd-wallet-insufficient");while(images.MoveNext())yield return images.Current;
        g.BackToPacks();g.Run.Credit(2000);g.SelectPack(1);g.SelectPayment(PaymentMethod.Wallet);g.ContinueCheckout();g.ConfirmTopUp();g.hasFocus=true;g.AdvancePayment(SceneRoot.PaymentDuration);g.hasFocus=false;
        g.ShowPaymentActivity();images=Views("sgd-activity");while(images.MoveNext())yield return images.Current;
        g.BackToPacks();g.SelectPayment(PaymentMethod.VirtualCard);g.Run.Payments.cardCharged=9800;g.ContinueCheckout();g.ConfirmTopUp();
        images=Views("sgd-card-insufficient");while(images.MoveNext())yield return images.Current;
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
            Graphics.Blit(target,readback,new Vector2(1,-1),new Vector2(0,1));
            RenderTexture.active=readback;
            var image=new Texture2D(width,height,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();
            string dir=Path.GetFullPath("../artifacts/payment-previews");Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Combine(dir,name+".png"),image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
            RenderTexture.ReleaseTemporary(readback);
            Debug.Log("CAPTURED: "+name);
        }
        finally{RenderTexture.active=before;target.Release();UnityEngine.Object.DestroyImmediate(target);}
    }
}
