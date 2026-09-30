using System;
using UnityEngine;

namespace JumpNotIncluded
{
    // An uninterrupted, focus-aware delivery. This visual rig never equips or moves the real player.
    public sealed class MechArrival : MonoBehaviour
    {
        public const float Duration=6.2f;
        public float Elapsed {get;private set;}
        public bool Finished {get;private set;}
        public float Progress=>Mathf.Clamp01(Elapsed/Duration);
        public string Stage=>Elapsed<3.05f?"ORBITAL DELIVERY":Elapsed<4.95f?"CONSCIOUSNESS UPLINK":"SYNCHRONIZATION COMPLETE";
        public Vector3 LandingPosition {get;private set;}
        public Bounds HullBounds=>hull!=null?MechArt.VisibleBounds(hull,3):new Bounds();
        public bool AudioPlaying=>ignition!=null&&(ignition.isPlaying||thruster.isPlaying||signal.isPlaying);
        private SceneRoot game;
        private Action completed;
        private Camera view;
        private Vector3 cameraPosition,cinemaPosition,marioCenter;
        private float cameraSize,cinemaSize;
        private bool marioVisible,wingVisible,gunVisible,audioStarted,audioPaused,restored,cameraRestored;
        private bool transferSound,completeSound;
        private readonly bool[] resumeSource=new bool[3];
        private SpriteRenderer hull,marioEcho;
        private Transform rig;
        private Sprite[] frames;
        private Material lightMaterial;
        private Mesh pixelMesh;
        private MechExhaust exhaust;
        private float washSurface;
        private bool hasWashSurface;
        private MeshRenderer scan;
        private MeshRenderer[] corePixels,marioPixels,dust,motes;
        private AudioSource ignition,thruster,signal;
        private Font captionFont;
        private MaterialPropertyBlock glowTint;
        private const float PixelSize=1f/16;
        private static readonly Color White=new Color32(244,255,255,255);
        private static readonly Color Cyan=new Color32(76,204,255,255);
        private static readonly Color Blue=new Color32(39,91,210,255);

        public void Init(SceneRoot root,Action onCompleted)
        {
            game=root;completed=onCompleted;view=game.cameraView;
            glowTint=new MaterialPropertyBlock();
            cameraPosition=view.transform.position;cameraSize=view.orthographicSize;
            var player=game.player;
            marioVisible=player.visual.enabled;wingVisible=player.wings.enabled;gunVisible=player.gun.gameObject.activeSelf;
            player.wings.enabled=false;player.gun.gameObject.SetActive(false);
            marioCenter=player.visual.bounds.center;
            Vector3 at=player.body.position;
            float x=Mathf.Clamp(at.x+3,2,game.level.length-2);
            float y=Mathf.Clamp(at.y+2,2.25f,6.3f);
            // Deliver above nearby pipes/blocks, never materialize the playable hull inside terrain.
            foreach(var solid in Physics2D.OverlapBoxAll(new Vector2(x,3.5f),new Vector2(1.5f,7),0,1<<8))
                if(!solid.isTrigger)y=Mathf.Max(y,solid.bounds.max.y+MechSuit.HullHeight*.5f+.25f);
            LandingPosition=new Vector3(x,Mathf.Min(y,MechSuit.MaxAltitude),0);
            // Keep both the consciousness origin and the elevated hull inside the letterbox.
            // Pipes and high bricks can raise the delivery point well above Mario's head.
            float low=marioCenter.y-.9f,high=LandingPosition.y+3.4f;
            cinemaSize=Mathf.Max(5.6f,(high-low)/1.45f);
            float cinemaY=Mathf.Clamp(LandingPosition.y+1.4f,high-cinemaSize*.785f,low+cinemaSize*.665f);
            float halfWidth=cinemaSize*view.aspect;
            cinemaPosition=new Vector3(Mathf.Clamp(at.x+3,halfWidth,Mathf.Max(halfWidth,game.level.length-halfWidth)),
                cinemaY,cameraPosition.z);
            lightMaterial=new Material(Shader.Find("Sprites/Default")){mainTexture=Texture2D.whiteTexture};
            pixelMesh=MakePixelMesh();
            rig=new GameObject("Orbital delivery / cinematic replica").transform;rig.SetParent(transform,false);
            hull=Sprite("Starfield camouflage / descent",rig,null,Color.white,35);
            hull.transform.localPosition=Vector3.down*1.075f;
            hull.transform.localScale=Vector3.one;hull.sharedMaterial=game.assets.mechPixelMaterial;
            frames=MechArt.CreateFrames(game.assets);hull.sprite=frames[3];
            marioEcho=Sprite("Mario / consciousness echo",transform,player.visual.sprite,Color.white,39);
            marioEcho.sharedMaterial=player.visual.sharedMaterial;marioEcho.flipX=player.visual.flipX;
            marioEcho.transform.position=player.visual.transform.position;
            marioEcho.transform.localScale=player.visual.transform.lossyScale;marioEcho.enabled=false;
            exhaust=rig.gameObject.AddComponent<MechExhaust>();exhaust.Init(rig,30);
            var surface=Physics2D.Raycast(LandingPosition+Vector3.down*.5f,Vector2.down,8,1<<8);
            hasWashSurface=surface.collider!=null;washSurface=surface.point.y;
            scan=Block("Synchronization / one-pixel scan",transform,38);
            corePixels=new MeshRenderer[9];for(int i=0;i<corePixels.Length;i++)corePixels[i]=Block("Reactor / pixel "+i,transform,37);
            marioPixels=new MeshRenderer[8];for(int i=0;i<marioPixels.Length;i++)marioPixels[i]=Block("Consciousness / pixel charge "+i,transform,40);
            dust=new MeshRenderer[18];for(int i=0;i<dust.Length;i++)dust[i]=Block("Engine wash / pixel "+i,transform,29);
            motes=new MeshRenderer[34];for(int i=0;i<motes.Length;i++)motes[i]=Block("Consciousness / photon "+i,transform,42);
            ignition=Source("Delivery / orbital descent",game.assets.mechDescent,false);
            thruster=Source("Delivery / braking engines",game.assets.mechThrustLoop,true);
            signal=Source("Delivery / uplink and synchronization",null,false);
            captionFont=Font.CreateDynamicFontFromOSFont(new[]{"Segoe UI","Arial"},32);
            RenderSequence();UpdateSound();
        }

        private SpriteRenderer Sprite(string label,Transform parent,Sprite sprite,Color color,int order)
        {
            var go=new GameObject(label);go.transform.SetParent(parent,false);var sr=go.AddComponent<SpriteRenderer>();
            sr.sprite=sprite;sr.color=color;sr.sortingOrder=order;return sr;
        }
        private MeshRenderer Block(string label,Transform parent,int order)
        {
            var go=new GameObject(label);go.transform.SetParent(parent,false);
            go.AddComponent<MeshFilter>().sharedMesh=pixelMesh;var renderer=go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial=lightMaterial;renderer.sortingOrder=order;return renderer;
        }
        private Mesh MakePixelMesh()
        {
            var mesh=new Mesh{name="Delivery / opaque pixel quad"};
            mesh.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0)};
            mesh.uv=new[]{Vector2.one*.5f,Vector2.one*.5f,Vector2.one*.5f,Vector2.one*.5f};
            mesh.colors=new[]{Color.white,Color.white,Color.white,Color.white};
            mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateBounds();return mesh;
        }
        private void PixelBlock(MeshRenderer renderer,Vector3 center,int width,int height,Color color)
        {
            float w=width*PixelSize,h=height*PixelSize;
            center.x=Mathf.Round((center.x-w*.5f)/PixelSize)*PixelSize+w*.5f;
            center.y=Mathf.Round((center.y-h*.5f)/PixelSize)*PixelSize+h*.5f;
            renderer.transform.position=center;
            Vector3 parentScale=renderer.transform.parent.lossyScale;
            renderer.transform.localScale=new Vector3(w/Mathf.Max(.0001f,Mathf.Abs(parentScale.x)),h/Mathf.Max(.0001f,Mathf.Abs(parentScale.y)),1);
            renderer.enabled=true;
            glowTint.SetColor("_Color",color);renderer.SetPropertyBlock(glowTint);
        }
        private AudioSource Source(string label,AudioClip clip,bool loop)
        {
            var go=new GameObject(label);go.transform.SetParent(transform,false);var source=go.AddComponent<AudioSource>();
            source.playOnAwake=false;source.spatialBlend=0;source.outputAudioMixerGroup=game.assets.worldGroup;
            source.clip=clip;source.loop=loop;return source;
        }
        private void Update()
        {
            if(game==null||Finished)return;
            UpdateSound();
            if(!game.hasFocus)return;
            float delta=Time.unscaledDeltaTime;
#if UNITY_EDITOR
            // Offline QA records the actual mixer and cameras at this fixed frame interval.
            if(Time.captureDeltaTime>0)delta=Time.captureDeltaTime;
#endif
            Elapsed=Mathf.Min(Duration,Elapsed+Mathf.Min(delta,.05f));
            RenderSequence();
            if(Elapsed>=Duration)
            {
                Finished=true;StopAudio();RestoreCamera();
                var callback=completed;completed=null;callback?.Invoke();RestorePlayer();Destroy(gameObject);
            }
        }
        private void LateUpdate()
        {
            if(game==null||Finished||view==null)return;
            // Camera motion, shake and all visual effects use the same paused cinematic clock.
            float framing=Smooth(Mathf.Clamp01(Elapsed/.6f));
            float release=Smooth(Mathf.Clamp01((Elapsed-5.55f)/.65f));
            float blend=framing*(1-release);
            Vector3 position=Vector3.Lerp(cameraPosition,cinemaPosition,blend);
            float shake=Mathf.Clamp01(1-Mathf.Abs(Elapsed-2.35f)/.45f)*.055f;
            position+=new Vector3(Mathf.Sin(Elapsed*103),Mathf.Sin(Elapsed*79),0)*shake;
            view.transform.position=position;view.orthographicSize=Mathf.Lerp(cameraSize,cinemaSize,blend);
        }
        private static float Smooth(float value)=>value*value*(3-2*value);
        private Vector3 Core=>hull.transform.TransformPoint(MechArt.CoreLocal(3));
        private Vector3 StreamPoint(float progress)
        {
            Vector3 point=Vector3.Lerp(marioCenter,Core,progress);
            point.y+=Mathf.Sin(progress*Mathf.PI)*1.05f;
            return point;
        }
        private void RenderSequence()
        {
            float descent=Mathf.Clamp01((Elapsed-.25f)/2.2f);
            rig.position=LandingPosition+Vector3.up*(13*Mathf.Pow(1-descent,2.3f)+(descent>=1?Mathf.Sin((Elapsed-2.45f)*4)*.04f:0));
            // The exact pixel-art hover pose, scale and attachment points used by the playable suit.
            hull.sprite=frames[3];
            float braking=Mathf.Clamp01((descent-.25f)/.65f);
            float engine=Elapsed<2.45f?Mathf.Lerp(.38f,1,braking):Mathf.Lerp(.75f,.38f,Mathf.Clamp01((Elapsed-2.45f)/.7f));
            Vector3 left=rig.InverseTransformPoint(hull.transform.TransformPoint(MechArt.SoleLocal(3,false)));
            Vector3 right=rig.InverseTransformPoint(hull.transform.TransformPoint(MechArt.SoleLocal(3,true)));
            exhaust.Render(Elapsed,engine,left,right,Vector2.down);
            // Only the two-dimensional sprite moves continuously; every effect is quantized to 16 PPU.
            int frame=Mathf.FloorToInt(Elapsed*12);
            float pixelTime=frame/12f;
            float wash=hasWashSurface?Mathf.Clamp01(1-(rig.position.y-MechSuit.HullHeight*.5f-washSurface)/3)*engine:0;
            for(int i=0;i<dust.Length;i++)
            {
                float age=Mathf.Repeat(pixelTime*.9f+i*.137f,1),side=i%2==0?-1:1;
                dust[i].enabled=wash>.12f&&age<.7f&&i%3!=frame%3;
                if(!dust[i].enabled)continue;
                Vector3 point=new Vector3(LandingPosition.x+side*(.15f+age*1.5f),washSurface+PixelSize*(age<.35f?2:1),0);
                PixelBlock(dust[i],point,i%3==0?2:1,1,i%2==0?new Color32(165,178,190,255):new Color32(99,119,140,255));
            }
            float transfer=Mathf.Clamp01((Elapsed-3.05f)/1.8f);
            if(Elapsed>=3.05f)game.player.visual.enabled=false;
            marioEcho.enabled=Elapsed>=3.05f&&transfer<.5f&&(transfer<.2f||frame%2==0);
            marioEcho.color=frame%2==0?White:Cyan;
            marioEcho.transform.localScale=game.player.visual.transform.lossyScale;
            Vector3 echo=game.player.visual.transform.position+Vector3.up*(Mathf.Floor(transfer*4)*PixelSize);
            echo.x=Mathf.Round(echo.x/PixelSize)*PixelSize;echo.y=Mathf.Round(echo.y/PixelSize)*PixelSize;
            marioEcho.transform.position=echo;
            for(int i=0;i<marioPixels.Length;i++)
            {
                marioPixels[i].enabled=Elapsed>=2.75f&&transfer<.6f&&(i+frame)%3!=0;
                if(!marioPixels[i].enabled)continue;
                float side=i%2==0?-1:1;
                Vector3 point=marioCenter+new Vector3(side*(3-i%3)*PixelSize,(i/2-1)*3*PixelSize,0);
                point=Vector3.Lerp(point,StreamPoint(.18f),Mathf.Clamp01(transfer*2));
                PixelBlock(marioPixels[i],point,1,1,i%2==0?White:Cyan);
            }
            float pixelTransfer=Mathf.Clamp01((pixelTime-3.05f)/1.8f);
            for(int i=0;i<motes.Length;i++)
            {
                float offset=i/(float)motes.Length;
                float travel=pixelTransfer*2.2f-offset*.95f;
                bool visible=Elapsed>=3.05f&&travel>=0&&travel<=1;
                motes[i].enabled=visible;if(!visible)continue;
                Vector3 point=StreamPoint(travel)+Vector3.up*((i%3-1)*PixelSize);
                int size=i%6==0?2:1;
                PixelBlock(motes[i],point,size,size,i%3==0?White:i%3==1?Cyan:Blue);
            }
            // A 3-pixel cross grows to 5 pixels for the synchronization beat, never a radial glow.
            bool syncFlash=Elapsed>=4.8f&&Elapsed<5.35f;
            int radius=syncFlash&&frame%3!=0?2:Elapsed>=3.05f?1:0;
            for(int i=0;i<corePixels.Length;i++)
            {
                int step=i==0?0:(i-1)/4+1,arm=i==0?0:(i-1)%4;
                corePixels[i].enabled=step<=radius;
                if(!corePixels[i].enabled)continue;
                Vector3 direction=arm==0?Vector3.left:arm==1?Vector3.right:arm==2?Vector3.up:Vector3.down;
                PixelBlock(corePixels[i],Core+direction*(step*PixelSize),1,1,step==0?White:step==1?Cyan:Blue);
            }
            scan.enabled=Elapsed>=4.65f&&Elapsed<5.55f;
            if(scan.enabled)
            {
                Bounds bounds=HullBounds;
                float scanHeight=Mathf.Lerp(bounds.min.y,bounds.max.y,Mathf.Clamp01((pixelTime-4.65f)/.9f));
                PixelBlock(scan,new Vector3(bounds.center.x,scanHeight,Core.z),Mathf.Max(1,Mathf.CeilToInt(bounds.size.x/PixelSize)),1,Cyan);
            }
        }
        public void OnFocusChanged(bool focus)
        {
            if(ignition==null||Finished)return;
            if(!focus)
            {
                if(!audioPaused)
                {
                    var sources=new[]{ignition,thruster,signal};
                    for(int i=0;i<sources.Length;i++){resumeSource[i]=sources[i].isPlaying;sources[i].Pause();}
                    audioPaused=true;
                }
                return;
            }
            if(audioPaused)
            {
                var sources=new[]{ignition,thruster,signal};
                for(int i=0;i<sources.Length;i++)if(resumeSource[i])sources[i].UnPause();
                audioPaused=false;
            }
        }
        private void UpdateSound()
        {
            OnFocusChanged(game.hasFocus);
            if(!game.hasFocus)return;
            float effects=game.audioDirector.world.volume;
            ignition.volume=effects*.65f;signal.volume=effects*.65f;
            thruster.volume=effects*Mathf.Lerp(.42f,.16f,Mathf.Clamp01((Elapsed-2.4f)/1.1f));
            thruster.pitch=Mathf.Lerp(.68f,1.08f,Mathf.Clamp01(Elapsed/2.3f));
            if(!audioStarted)
            {if(ignition.clip!=null)ignition.Play();if(thruster.clip!=null)thruster.Play();audioStarted=true;}
            if(Elapsed>=3.05f&&!transferSound)
            {transferSound=true;signal.pitch=1;if(game.assets.mechUplink!=null)signal.PlayOneShot(game.assets.mechUplink);}
            if(Elapsed>=4.95f&&!completeSound)
            {completeSound=true;signal.pitch=1;if(game.assets.mechReady!=null)signal.PlayOneShot(game.assets.mechReady);}
        }
        private void OnGUI()
        {
            if(game==null||Finished||captionFont==null)return;
            var matrix=GUI.matrix;var color=GUI.color;int depth=GUI.depth;
            GUI.matrix=Matrix4x4.identity;GUI.depth=-100;
            float width=Screen.width,height=Screen.height,unit=Mathf.Min(width/1600f,height/900f);
            float fade=Mathf.Clamp01(Elapsed/.3f)*(1-Mathf.Clamp01((Elapsed-5.8f)/.4f));
            Rect top=new Rect(0,0,width,96*unit),bottom=new Rect(0,height-150*unit,width,150*unit);
            GUI.color=new Color(.012f,.025f,.055f,.95f*fade);GUI.DrawTexture(top,Texture2D.whiteTexture);GUI.DrawTexture(bottom,Texture2D.whiteTexture);
            GUI.color=Color.white;
            var title=new GUIStyle(GUI.skin.label){font=captionFont,fontSize=Mathf.RoundToInt(31*unit),alignment=TextAnchor.MiddleCenter,fontStyle=FontStyle.Bold};
            var detail=new GUIStyle(title){fontSize=Mathf.RoundToInt(17*unit),fontStyle=FontStyle.Normal};
            var brand=new GUIStyle(detail){fontSize=Mathf.RoundToInt(16*unit)};
            title.normal.textColor=new Color(.93f,.98f,1,fade);detail.normal.textColor=new Color(.64f,.8f,.89f,fade);
            brand.normal.textColor=new Color(.95f,.69f,.3f,fade);
            GUI.Label(new Rect(0,21*unit,width,30*unit),"SL CHEATER  /  WAR GOD REPLICA",brand);
            GUI.Label(new Rect(0,49*unit,width,24*unit),"STARFIELD CAMOUFLAGE  //  MOON KILLER",detail);
            GUI.Label(new Rect(0,height-134*unit,width,43*unit),Stage,title);
            string caption=Elapsed<3.05f?"Orbital authority has arrived. Please remain beneath your purchase.":
                Elapsed<4.95f?"Your consciousness becomes light. A new body awaits.":"Consciousness synchronized. Flight systems are yours.";
            GUI.Label(new Rect(20*unit,height-84*unit,width-40*unit,30*unit),caption,detail);
            GUI.color=new Color(.15f,.27f,.35f,fade);GUI.DrawTexture(new Rect(width*.3f,height-34*unit,width*.4f,2*unit),Texture2D.whiteTexture);
            GUI.color=new Color(.38f,.94f,1,fade);GUI.DrawTexture(new Rect(width*.3f,height-34*unit,width*.4f*Progress,2*unit),Texture2D.whiteTexture);
            GUI.matrix=matrix;GUI.color=color;GUI.depth=depth;
        }
        private void RestoreCamera()
        {if(cameraRestored)return;cameraRestored=true;if(view!=null){view.transform.position=cameraPosition;view.orthographicSize=cameraSize;}}
        private void RestorePlayer()
        {
            if(restored||game==null||game.player==null)return;restored=true;
            bool equipped=game.player.MechActive;
            game.player.visual.enabled=!equipped&&marioVisible;
            game.player.wings.enabled=!equipped&&wingVisible;
            game.player.gun.gameObject.SetActive(!equipped&&gunVisible);
        }
        private void StopAudio()
        {if(ignition!=null)ignition.Stop();if(thruster!=null)thruster.Stop();if(signal!=null)signal.Stop();}
        private void OnDisable(){StopAudio();}
        private void OnDestroy()
        {
            StopAudio();RestoreCamera();RestorePlayer();
            if(frames!=null)foreach(var frame in frames)if(frame!=null)Destroy(frame);
            if(lightMaterial!=null)Destroy(lightMaterial);if(pixelMesh!=null)Destroy(pixelMesh);if(captionFont!=null)Destroy(captionFont);
        }
    }
}
