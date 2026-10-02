using System.Collections.Generic;
using UnityEngine;

namespace JumpNotIncluded
{
    public enum CastleHazardKind {Lava,FireBar,Crusher,LavaJet}

    public class CastleHazard : MonoBehaviour
    {
        public string id;
        public CastleHazardKind Kind {get;private set;}
        public bool Destroyed {get;private set;}
        public bool Destructible=>Kind==CastleHazardKind.FireBar||Kind==CastleHazardKind.Crusher;
        public float CycleTime=>age;
        public float ActiveHeight {get;private set;}
        private SceneRoot game;
        private Vector2 size,origin;
        private float age,contactCooldown;
        private int previousStage=-1;
        private readonly List<SpriteRenderer> pieces=new List<SpriteRenderer>();
        private BoxCollider2D box;
        private Transform moving;

        public void Init(SceneRoot root,string key,CastleHazardKind kind,Vector2 center,Vector2 dimensions,float phase=0)
        {
            game=root;id=key;Kind=kind;origin=center;size=new Vector2(Mathf.Max(.1f,dimensions.x),Mathf.Max(.1f,dimensions.y));
            transform.position=center;transform.localScale=Vector3.one;age=Mathf.Max(0,phase);
            if(game==null||game.session==null){enabled=false;return;}
            if(game.Run.collected.Contains(id+".destroyed")){Destroyed=true;Destroy(gameObject);return;}
            if(kind==CastleHazardKind.FireBar)
            {
                var anchor=Visual("Rotating firebar anchor","gate",Vector2.zero,new Vector2(.72f,.72f),5);
                anchor.color=new Color(.65f,.55f,.6f);
                moving=new GameObject("Rotating fire arm").transform;moving.SetParent(transform,false);
                int count=Mathf.CeilToInt(size.x/.6f);
                for(int i=0;i<count;i++)
                {
                    var flame=Visual("Firebar flame "+i,"fire",new Vector2((i+1)*size.x/count,0),Vector2.one,7,moving);
                    var c=flame.gameObject.AddComponent<CircleCollider2D>();c.radius=.3f;c.isTrigger=true;pieces.Add(flame);
                }
            }
            else if(kind==CastleHazardKind.Crusher)
            {
                for(float y=.5f;y<4;y+=1)Visual("Crusher suspension chain","chain",new Vector2(0,y),Vector2.one,2);
                var crusher=Visual("Spiked descending crusher","crusher",Vector2.zero,Vector2.one*size.x/2,8);
                moving=crusher.transform;pieces.Add(crusher);
                box=crusher.gameObject.AddComponent<BoxCollider2D>();box.size=Vector2.one*1.82f;box.isTrigger=true;
            }
            else
            {
                box=gameObject.AddComponent<BoxCollider2D>();box.isTrigger=true;
                if(kind==CastleHazardKind.Lava)
                {
                    box.size=size;
                    // Surface remains at the top of the true damaging lava rectangle.
                    int columns=Mathf.CeilToInt(size.x),rows=Mathf.CeilToInt(size.y);
                    for(int row=0;row<rows;row++)for(int col=0;col<columns;col++)
                    {
                        float width=Mathf.Min(1,size.x-col),height=Mathf.Min(1,size.y-row);
                        var lava=Visual("Lava "+col+","+row,"lava",
                            new Vector2(-size.x/2+col+width/2,size.y/2-row-height/2),new Vector2(width,height),4);
                        if(row>0)lava.color=new Color(.65f,.33f,.25f);
                        pieces.Add(lava);
                    }
                }
                else
                {
                    var jet=Visual("Erupting lava column","jet",Vector2.zero,new Vector2(size.x,0),7);
                    pieces.Add(jet);box.enabled=false;
                    var mouth=Visual("Lava jet vent","gate",new Vector2(0,-.18f),new Vector2(size.x+.25f,.35f),6);
                    mouth.color=new Color(.95f,.4f,.3f);
                }
            }
            Draw(false);
        }

        private SpriteRenderer Visual(string label,string key,Vector2 position,Vector2 scale,int order,Transform parent=null)
        {
            var go=new GameObject(label);go.transform.SetParent(parent!=null?parent:transform,false);
            go.transform.localPosition=position;go.transform.localScale=new Vector3(scale.x,scale.y,1);
            var sr=go.AddComponent<SpriteRenderer>();sr.sprite=CastleArt.Sprite(key);sr.sortingOrder=order;return sr;
        }

        private void FixedUpdate()
        {
            if(Destroyed||game==null||!game.Playing||!game.hasFocus)return;
            age+=Time.fixedDeltaTime;contactCooldown-=Time.fixedDeltaTime;Draw(true);TouchPlayer();
        }

        private void Draw(bool cues)
        {
            int frame=Mathf.FloorToInt(age*8)%4,stage=0;
            if(Kind==CastleHazardKind.FireBar)
            {
                moving.localRotation=Quaternion.Euler(0,0,age*77);
                foreach(var sr in pieces){sr.sprite=CastleArt.Sprite("fire",frame);sr.transform.rotation=Quaternion.identity;}
            }
            else if(Kind==CastleHazardKind.Crusher)
            {
                float t=age%4.4f,drop;
                if(t<.8f){stage=0;drop=0;}
                else if(t<1.45f){stage=1;drop=0;}
                else if(t<1.8f){stage=2;drop=Mathf.Pow((t-1.45f)/.35f,2);}
                else if(t<2.45f){stage=3;drop=1;}
                else {stage=4;drop=1-(t-2.45f)/1.95f;}
                moving.localPosition=new Vector3(stage==1?(Mathf.FloorToInt(age*25)%2==0?-.0625f:.0625f):0,-size.y*drop,0);
                pieces[0].color=stage==1?new Color(1,.5f,.35f):Color.white;
                if(cues&&stage==3&&previousStage!=3)Cue("break");
                if(cues&&stage==1&&previousStage!=1)Cue("bump");
            }
            else if(Kind==CastleHazardKind.Lava)
            {foreach(var sr in pieces)sr.sprite=CastleArt.Sprite("lava",frame);}
            else
            {
                float t=age%3.6f,fraction=0;
                if(t<1.5f){stage=0;fraction=.035f+.018f*(frame%2);}
                else if(t<1.85f){stage=1;fraction=(t-1.5f)/.35f;}
                else if(t<2.75f){stage=2;fraction=1;}
                else if(t<3.2f){stage=3;fraction=1-(t-2.75f)/.45f;}
                else stage=4;
                ActiveHeight=size.y*fraction;
                var sr=pieces[0];sr.enabled=fraction>0;sr.sprite=CastleArt.Sprite("jet",frame);
                sr.transform.localPosition=new Vector3(0,ActiveHeight/2,0);sr.transform.localScale=new Vector3(size.x,ActiveHeight/2,1);
                box.enabled=stage>0&&stage<4&&ActiveHeight>.15f;
                box.offset=Vector2.up*ActiveHeight/2;box.size=new Vector2(size.x*.66f,Mathf.Max(.1f,ActiveHeight));
                if(cues&&stage==1&&previousStage!=1)Cue("bowser-fire");
            }
            previousStage=stage;
        }

        private void TouchPlayer()
        {
            var player=game.player;
            if(player==null||player.box==null||player.MechActive||contactCooldown>0)return;
            var bounds=player.box.bounds;bool touching=false;
            if(Kind==CastleHazardKind.FireBar)
            {
                foreach(var sr in pieces)
                {
                    var p=sr.transform.position;var closest=bounds.ClosestPoint(p);
                    if(((Vector2)(p-closest)).sqrMagnitude<.09f){touching=true;break;}
                }
            }
            else if(Kind==CastleHazardKind.Crusher)
            {
                var hit=new Bounds(moving.position,new Vector3(size.x*.91f,size.x*.91f,1));touching=hit.Intersects(bounds);
            }
            else if(box!=null&&box.enabled)
            {
                var hit=new Bounds((Vector2)transform.position+box.offset,new Vector3(box.size.x,box.size.y,1));touching=hit.Intersects(bounds);
            }
            if(!touching)return;
            contactCooldown=.15f;
            if(Kind==CastleHazardKind.Lava)game.KillPlayer("The lava does not recognize your basic subscription.");
            else player.Hit(false,Kind==CastleHazardKind.Crusher?"The castle has compressed your remaining consumer rights.":"This firewall is available only to premium survivors.");
        }

        public void BreakByWeapon(bool mech)
        {
            if(Destroyed||game==null||!game.Playing||!game.hasFocus||Kind==CastleHazardKind.Lava||!mech&&!Destructible)return;
            Destroyed=true;game.AddScore(id+".destroyed",150);Cue("break");
            CastleDebris.Spawn(game,moving!=null?(Vector2)moving.position:(Vector2)transform.position,new Color(1,.48f,.15f),12);
            foreach(var c in GetComponentsInChildren<Collider2D>())c.enabled=false;
            gameObject.SetActive(false);Destroy(gameObject);
        }

        private void Cue(string key)
        {
            if(game.events==null||game.cameraView==null)return;
            var view=game.cameraView.WorldToViewportPoint(moving!=null?moving.position:transform.position);
            if(view.z>0&&view.x>-.1f&&view.x<1.1f&&view.y>-.2f&&view.y<1.2f)game.events.Sound(key);
        }
    }
}
