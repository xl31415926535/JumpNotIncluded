using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;

namespace JumpNotIncluded.EditorTools
{
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        private const string Data="Assets/GameData";
        private static readonly string[] SceneNames={"MainMenu","Loading","World01","World02"};
        static ProjectSetup()
        {
            EditorApplication.delayCall+=()=>
            {
                if(!Application.isBatchMode && !EditorApplication.isCompiling && !EditorApplication.isPlayingOrWillChangePlaymode)
                {try{Setup();}catch(Exception e){Debug.LogException(e);}}
            };
        }
        [MenuItem("Tools/Jump Not Included/Setup project")]
        public static void Setup()
        {
            var existing=AssetDatabase.LoadAssetAtPath<GameAssets>("Assets/Resources/GameAssets.asset");
            if(existing!=null)
            {
                if(existing.revision<9)
                {
                    if(existing.revision<6){CreateStates(existing);CreateAds(existing);}
                    if(existing.revision<7)CreateProducts(existing);
                    CreateSprites(existing);CreateSounds(existing);existing.revision=9;
                    EditorUtility.SetDirty(existing);AssetDatabase.SaveAssets();
                    Debug.Log("JNI assets updated: red growth mushroom and Starman music.");
                }
                return;
            }
            foreach(string p in new[]{Data,Data+"/Sprites",Data+"/FSM",Data+"/Products","Assets/Scenes","Assets/Resources"})Directory.CreateDirectory(p);
            AssetDatabase.Refresh();
            PlayerSettings.companyName="Student Arcade";PlayerSettings.productName="Jump Not Included";
            PlayerSettings.bundleVersion="1.11.2";PlayerSettings.colorSpace=ColorSpace.Gamma;
            PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;
            PlayerSettings.runInBackground=false;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            var settings=AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if(settings.Length>0)
            {
                var so=new SerializedObject(settings[0]);var active=so.FindProperty("activeInputHandler");
                if(active!=null){active.intValue=1;so.ApplyModifiedPropertiesWithoutUndo();}
            }
            EditorSettings.defaultBehaviorMode=EditorBehaviorMode.Mode2D;
            var bank=ScriptableObject.CreateInstance<GameAssets>();
            CreateSprites(bank);
            var shader=Shader.Find("JumpNotIncluded/ChromaSprite");
            if(shader==null)throw new Exception("Chroma sprite shader was not imported.");
            bank.blueKey=new Material(shader){name="Blue background key"};bank.blueKey.SetColor("_KeyColor",new Color(0,136/255f,1));
            bank.greenKey=new Material(shader){name="Green background key"};bank.greenKey.SetColor("_KeyColor",Color.green);
            AssetDatabase.CreateAsset(bank.blueKey,Data+"/BlueKey.mat");AssetDatabase.CreateAsset(bank.greenKey,Data+"/GreenKey.mat");
            CreateSounds(bank);bank.revision=9;
            CreateStates(bank);CreateProducts(bank);CreateAds(bank);CreateMixer(bank);
            var session=ScriptableObject.CreateInstance<RunState>();session.NewRun();
            var events=ScriptableObject.CreateInstance<GameEvents>();
            AssetDatabase.CreateAsset(session,"Assets/Resources/RunState.asset");
            AssetDatabase.CreateAsset(events,"Assets/Resources/Events.asset");
            AssetDatabase.CreateAsset(bank,"Assets/Resources/GameAssets.asset");
            AssetDatabase.SaveAssets();
            foreach(string name in SceneNames)
            {
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var root=new GameObject("SceneRoot — "+name).AddComponent<SceneRoot>();
                root.assets=bank;root.session=session;root.events=events;
                EditorSceneManager.SaveScene(scene,"Assets/Scenes/"+name+".unity");
            }
            EditorBuildSettings.scenes=SceneNames.Select(n=>new EditorBuildSettingsScene("Assets/Scenes/"+n+".unity",true)).ToArray();
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();
            Debug.Log("Jump Not Included project setup complete.");
        }
        private static void CreateSprites(GameAssets bank)
        {
            var sprites=new List<NamedSprite>();
            var mario=Texture("mario");var enemies=Texture("enemies");var terrain=Texture("world");
            Sprite Add(string key,Texture2D texture,int x,int y,int w,int h,bool feet=false)
            {
                var sprite=UnityEngine.Sprite.Create(texture,new Rect(x,texture.height-y-h,w,h),feet?new Vector2(.5f,0):Vector2.one*.5f,16,0,SpriteMeshType.FullRect);
                sprite.name=key;
                string path=Data+"/Sprites/"+key+".asset";
                var previous=AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if(previous==null)AssetDatabase.CreateAsset(sprite,path);
                else
                {
                    EditorUtility.CopySerialized(sprite,previous);UnityEngine.Object.DestroyImmediate(sprite);
                    sprite=previous;EditorUtility.SetDirty(sprite);
                }
                sprites.Add(new NamedSprite{key=key,sprite=sprite});return sprite;
            }
            for(int r=0;r<3;r++)
            {
                string prefix=r==0?"small":r==1?"big":"fire";int y=r==0?0:r==1?52:122;int h=r==0?16:32;
                Add(prefix+"-idle",mario,180,y,16,h,true);
                int[] walk=r==2?new[]{52,77,128}:r==1?new[]{60,90,121}:new[]{60,89,121};
                for(int i=0;i<3;i++)Add(prefix+"-walk"+i,mario,walk[i],y,16,h,true);
                Add(prefix+"-jump",mario,r==2?27:29,y,18,h,true);
            }
            Add("goomba0",enemies,0,4,16,16,true);Add("goomba1",enemies,30,4,16,16,true);Add("goomba-dead",enemies,60,8,16,8,true);
            Add("bowser0",enemies,0,210,40,34,true);Add("bowser1",enemies,40,210,40,34,true);
            Add("bowser-flame",enemies,103,249,28,11);
            var wings=Texture("monarch-wings","png",FilterMode.Bilinear);
            Add("wings",wings,0,0,wings.width,wings.height);
            Add("ground",terrain,373,124,16,16);Add("brick",terrain,373,47,16,16);
            Add("question",terrain,372,160,16,16);Add("spent",terrain,373,65,16,16);
            Add("mushroom",terrain,71,43,16,16);Add("flower",terrain,52,64,16,16);
            Add("star",terrain,52,103,16,16);Add("coin",terrain,427,163,10,14);
            Add("pipe",terrain,613,46,32,32);Add("cloud",terrain,46,198,48,24);
            Add("hill",terrain,48,176,48,19,true);Add("hill-large",terrain,99,160,80,35,true);
            Add("castle",terrain,270,216,80,82);
            var solidTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Data+"/Solid.asset");
            if(solidTexture==null)
            {
                solidTexture=new Texture2D(16,16,TextureFormat.RGBA32,false){name="Solid"};
                solidTexture.SetPixels(Enumerable.Repeat(Color.white,256).ToArray());solidTexture.Apply();
                AssetDatabase.CreateAsset(solidTexture,Data+"/Solid.asset");
            }
            bank.solid=Add("solid",solidTexture,0,0,16,16);
            bank.sprites=sprites.ToArray();
        }
        private static void CreateSounds(GameAssets bank)
        {
            string[,] sounds={
                {"theme","01-main-theme-overworld.mp3"},{"underground","02-underworld.mp3"},
                {"star","05-starman.mp3"},
                {"jump","smb_jump-small.wav"},{"coin","smb_coin.wav"},{"stomp","smb_stomp.wav"},
                {"bump","smb_bump.wav"},{"error","smb_bump.wav"},{"powerup","smb_powerup.wav"},
                {"break","smb_breakblock.wav"},{"appear","smb_powerup_appears.wav"},{"fire","smb_fireball.wav"},
                {"pause","smb_pause.wav"},{"death","08-you-re-dead.mp3"},
                {"complete","06-level-complete.mp3"},{"expiry","smb_pipe_powerdown.wav"},
                {"bowser-fire","smb_bowserfire.wav"},{"bowser-fall","smb_bowserfalls.wav"}};
            bank.sounds=new NamedAudio[sounds.GetLength(0)];
            for(int i=0;i<bank.sounds.Length;i++)bank.sounds[i]=new NamedAudio{key=sounds[i,0],clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/"+sounds[i,1])};
        }
        private static Texture2D Texture(string name,string extension="png",FilterMode filter=FilterMode.Point)
        {
            string path="Assets/Art/"+name+"."+extension;
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Default;importer.filterMode=filter;
            importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;
            importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        private static void CreateAds(GameAssets bank)
        {
            bank.sutdAIAd=Texture("Ads/sutd-ai-mindset","jpg",FilterMode.Bilinear);
            bank.sutdRobotAd=Texture("Ads/sutd-school-for-innovators","jpg",FilterMode.Bilinear);
            bank.slCheaterAd=Texture("Ads/slcheater-war-god","png",FilterMode.Bilinear);
        }
        private static T Save<T>(T item,string file) where T:UnityEngine.Object
        {
            item.name=file;string path=Data+"/FSM/"+file+".asset";
            var existing=AssetDatabase.LoadAssetAtPath<T>(path);
            if(existing==null)AssetDatabase.CreateAsset(item,path);
            else
            {EditorUtility.CopySerialized(item,existing);UnityEngine.Object.DestroyImmediate(item);item=existing;}
            return item;
        }
        private static void CreateStates(GameAssets bank)
        {
            StateDefinition State(string name,int key,string sound)
            {
                var s=Save(ScriptableObject.CreateInstance<StateDefinition>(),name);s.key=key;
                if(!string.IsNullOrEmpty(sound))
                {var a=Save(ScriptableObject.CreateInstance<ActionDefinition>(),name+"_Enter");a.sound=sound;EditorUtility.SetDirty(a);s.enterActions=new[]{a};}
                return s;
            }
            StateTransition Signal(string from,string signal,StateDefinition target)
            {var d=Save(ScriptableObject.CreateInstance<DecisionDefinition>(),from+"_"+signal);d.signal=signal;EditorUtility.SetDirty(d);return new StateTransition{decision=d,target=target};}
            StateTransition Timer(string from,float seconds,StateDefinition target)
            {var d=Save(ScriptableObject.CreateInstance<DecisionDefinition>(),from+"_Expired");d.timed=true;d.seconds=seconds;EditorUtility.SetDirty(d);return new StateTransition{decision=d,target=target};}
            var small=State("Small",0,null);var big=State("Super",1,"powerup");var fire=State("Fire",2,"powerup");
            var hurt=State("HurtGrace",3,"expiry");var dead=State("Dead",4,null);
            dead.transitions=new StateTransition[0];
            var hurtSuper=State("HurtSuperGrace",5,"expiry");
            small.transitions=new[]{Signal("Small","mushroom",big),Signal("Small","flower",fire),Signal("Small","damage",dead)};
            big.transitions=new[]{Signal("Super","flower",fire),Signal("Super","damage",hurt)};
            fire.transitions=new[]{Signal("Fire","damage",hurtSuper),Signal("Fire","refund",big)};
            hurt.transitions=new[]{Signal("Hurt","mushroom",big),Signal("Hurt","flower",fire),Timer("Hurt",PlayerMotor.HurtDuration,small)};
            hurtSuper.transitions=new[]{Signal("HurtSuper","flower",fire),Timer("HurtSuper",PlayerMotor.HurtDuration,big)};
            bank.formStates=new[]{small,big,fire,hurt,dead,hurtSuper};
            var none=State("Buff_None",0,"expiry");var star=State("Buff_Star",1,"powerup");var vip=State("Buff_VIP",2,"powerup");
            none.transitions=new[]{Signal("None","star",star),Signal("None","vip",vip)};
            star.transitions=new[]{Signal("Star","star",star),Signal("Star","vip",vip),Timer("Star",6,none)};
            vip.transitions=new[]{Timer("VIP",8,none)};bank.buffStates=new[]{none,star,vip};
            foreach(var s in bank.formStates.Concat(bank.buffStates))EditorUtility.SetDirty(s);
        }
        private static void CreateProducts(GameAssets bank)
        {
            string[] title={"Jump DLC","Fire Flower","Mushroom ID","Master Guide","Monarch Wings","Gatling"};
            string[] desc={
                "A revolutionary new dimension! Escape the tyranny of flat ground and roam the glorious Z-axis. Gravity has finally met a paying customer.",
                "Become the sun! The royal Fire Flower places an apocalypse at your fingertips. Let every Goomba kneel before your incandescent majesty.",
                "Royal mycologists have abolished poison. One purchase rewrites nature: every treacherous mushroom becomes nourishment fit for a sovereign.",
                "Seize the sight of a thousand grandmasters! Hidden bricks, buried stars, secret fortunes: all bow before your divine gaze.",
                "A legendary relic of ancient Hallownest. Inherit the Pale King's divine legacy, unfurl Monarch Wings in the void, and command a second ascent.",
                "Abolish the level itself. Imperial fire erases foes, secret traps and pipes, then paves every abyss. Hold J and advance: victory belongs to you."};
            bank.products=new ProductDefinition[title.Length];
            for(int i=0;i<title.Length;i++)
            {
                string path=Data+"/Products/"+(Product)i+".asset";
                var p=AssetDatabase.LoadAssetAtPath<ProductDefinition>(path);
                if(p==null)
                {
                    p=ScriptableObject.CreateInstance<ProductDefinition>();p.product=(Product)i;p.price=RunModel.Price(p.product);
                    AssetDatabase.CreateAsset(p,path);
                }
                p.price=RunModel.Price(p.product);p.title=title[i];p.description=desc[i];EditorUtility.SetDirty(p);bank.products[i]=p;
            }
        }
        private static object Call(object target,string name,params object[] args)
        {
            var type=target as Type??target.GetType();
            var method=type.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance)
                .First(m=>m.Name==name&&m.GetParameters().Length==args.Length);
            return method.Invoke(target is Type?null:target,args);
        }
        private static Type EditorType(string name)
        {return AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType(name)).First(t=>t!=null);}
        private static void CreateMixer(GameAssets bank)
        {
            // These editor-only creation APIs are internal; the player uses public AudioMixer APIs.
            var controller=EditorType("UnityEditor.Audio.AudioMixerController");
            var mixer=(AudioMixer)Call(controller,"CreateMixerControllerAtPath",Data+"/GameAudio.mixer");
            var master=controller.GetProperty("masterGroup").GetValue(mixer);
            var music=Call(mixer,"CreateNewGroup","Music",false);
            var world=Call(mixer,"CreateNewGroup","WorldSFX",false);
            var ui=Call(mixer,"CreateNewGroup","UI_Ads",false);
            foreach(var group in new[]{music,world,ui})Call(mixer,"AddChildToParent",group,master);
            var effectType=EditorType("UnityEditor.Audio.AudioMixerEffectController");
            var effect=Activator.CreateInstance(effectType,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance,null,new object[]{"Lowpass"},null);
            Call(effect,"PreallocateGUIDs");AssetDatabase.AddObjectToAsset((UnityEngine.Object)effect,mixer);Call(music,"InsertEffect",effect,1);
            var guid=Call(effect,"GetGUIDForParameter","Cutoff freq");
            var prop=controller.GetProperty("exposedParameters");var element=prop.PropertyType.GetElementType();
            var value=Activator.CreateInstance(element);element.GetField("guid").SetValue(value,guid);element.GetField("name").SetValue(value,"ShopLowpass");
            var exposed=Array.CreateInstance(element,1);exposed.SetValue(value,0);prop.SetValue(mixer,exposed);
            EditorUtility.SetDirty(mixer);bank.mixer=mixer;bank.musicGroup=(AudioMixerGroup)music;bank.worldGroup=(AudioMixerGroup)world;bank.uiGroup=(AudioMixerGroup)ui;
        }
        [MenuItem("Tools/Jump Not Included/Validate rules")]
        public static void Validate()
        {
            RuleChecks.Run();
            var assets=AssetDatabase.LoadAssetAtPath<GameAssets>("Assets/Resources/GameAssets.asset");
            if(assets!=null)
            {
                if(assets.formStates.Length!=6||assets.buffStates.Length!=3)throw new Exception("FSM resources incomplete.");
                if(assets.sutdAIAd==null||assets.sutdRobotAd==null||assets.slCheaterAd==null)throw new Exception("Advertisement artwork missing.");
                if(assets.products.Length!=6||assets.Sprite("wings")==assets.solid||assets.Sprite("bowser0")==assets.solid)throw new Exception("Premium upgrade resources incomplete.");
                foreach(var sound in assets.sounds)if(sound.clip==null)throw new Exception("Missing sound: "+sound.key);
                if(assets.mixer==null||assets.musicGroup==null)throw new Exception("Mixer missing.");
                foreach(var state in assets.formStates.Concat(assets.buffStates))
                    foreach(var transition in state.transitions)
                        if(transition.decision==null||transition.target==null||
                            (transition.decision.timed?transition.decision.seconds<=0:string.IsNullOrEmpty(transition.decision.signal)))
                            throw new Exception("Unconfigured FSM transition: "+state.name);
            }
            Debug.Log("JNI validation passed: economy, refunds, reward deduplication, scores and resource references.");
        }
        [MenuItem("Tools/Jump Not Included/Play from MainMenu")]
        public static void Preview()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            Setup();Validate();
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            EditorApplication.delayCall+=()=>EditorApplication.isPlaying=true;
        }
        [MenuItem("Tools/Jump Not Included/Build Windows")]
        public static void Build()
        {
            Setup();Validate();Directory.CreateDirectory("Builds/Windows");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
                scenes=SceneNames.Select(n=>"Assets/Scenes/"+n+".unity").ToArray(),
                locationPathName="Builds/Windows/JumpNotIncluded.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);
            Debug.Log("JNI Windows build succeeded.");
        }
    }
}
