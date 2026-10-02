using System.Collections.Generic;
using UnityEngine;

namespace JumpNotIncluded
{
    // Unlike normal blocks, castle seals cannot be bumped, stomped or broken by being big.
    public class CastleBarrier : MonoBehaviour
    {
        public string id;
        public bool Destroyed {get;private set;}
        public int Health {get;private set;}=3;
        public bool FurnaceSeal {get;private set;}
        private SceneRoot game;
        private BoxCollider2D box;
        private float flash,contactHeat;
        private readonly List<SpriteRenderer> tiles=new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> cracks=new List<SpriteRenderer>();
        private readonly List<SpriteRenderer> embers=new List<SpriteRenderer>();
        private readonly HashSet<int> handledShots=new HashSet<int>();

        public void Init(SceneRoot root,string key,Vector2 center,Vector2 size,bool furnaceSeal=false)
        {
            game=root;id=key;FurnaceSeal=furnaceSeal;transform.position=center;transform.localScale=Vector3.one;
            gameObject.layer=8;
            if(game==null||game.session==null){enabled=false;return;}
            if(game.Run.collected.Contains(id+".destroyed")){Destroyed=true;Destroy(gameObject);return;}
            size=new Vector2(Mathf.Max(.25f,size.x),Mathf.Max(.25f,size.y));
            box=gameObject.AddComponent<BoxCollider2D>();box.size=size;
            int cols=Mathf.CeilToInt(size.x),rows=Mathf.CeilToInt(size.y);
            for(int y=0;y<rows;y++)for(int x=0;x<cols;x++)
            {
                float w=Mathf.Min(1,size.x-x),h=Mathf.Min(1,size.y-y);
                var go=new GameObject(furnaceSeal?"Furnace armor plate":"Castle masonry");go.transform.SetParent(transform,false);
                go.transform.localPosition=new Vector3(-size.x/2+x+w/2,-size.y/2+y+h/2,0);go.transform.localScale=new Vector3(w,h,1);
                var sr=go.AddComponent<SpriteRenderer>();sr.sprite=CastleArt.Sprite(furnaceSeal?"gate":"brick");sr.sortingOrder=5;tiles.Add(sr);
            }
            // Discrete luminous fractures are visible even when the impact flash has finished.
            for(int i=0;i<Mathf.CeilToInt(size.y*2);i++)
            {
                var go=new GameObject("Glowing fracture "+i);go.transform.SetParent(transform,false);
                go.transform.localPosition=new Vector3((i%2==0?-.1f:.1f)*Mathf.Min(size.x,2),-size.y/2+.22f+i*.45f,0);
                go.transform.localScale=Vector3.one;
                var sr=go.AddComponent<SpriteRenderer>();sr.sprite=CastleArt.Sprite("crack");sr.sortingOrder=6;sr.enabled=false;cracks.Add(sr);
            }
            if(furnaceSeal)for(int side=-1;side<=1;side+=2)for(float y=-size.y/2+.5f;y<size.y/2;y+=1)
            {
                var go=new GameObject("White-hot seal edge");go.transform.SetParent(transform,false);
                go.transform.localPosition=new Vector3(side*(size.x/2-.06f),y,0);
                go.transform.localScale=new Vector3(.55f,1,1);
                var sr=go.AddComponent<SpriteRenderer>();sr.sprite=CastleArt.Sprite("crack");sr.sortingOrder=7;embers.Add(sr);
            }
        }

        private void Update()
        {
            if(Destroyed||game==null||!game.Playing||!game.hasFocus)return;
            flash=Mathf.Max(0,flash-Time.deltaTime);
            foreach(var sr in tiles)sr.color=flash>0?new Color(1,.48f,.18f):Color.white;
            for(int i=0;i<cracks.Count;i++)cracks[i].enabled=Health<3&&(Health==1||i%2==0);
            foreach(var sr in embers)sr.color=Color.Lerp(new Color(1,.3f,.08f),new Color(1,1,.5f),Mathf.PingPong(Time.time*2,1));
        }

        private void FixedUpdate()
        {
            if(Destroyed||!FurnaceSeal||game==null||!game.Playing||!game.hasFocus||game.player==null)return;
            var player=game.player;
            if(player.MechActive){contactHeat=0;return;}
            var heatBounds=box.bounds;heatBounds.Expand(.1f);
            contactHeat=heatBounds.Intersects(player.box.bounds)?contactHeat+Time.fixedDeltaTime:0;
            // A blocked customer reaches the existing death/shop loop without a hidden suicide route.
            // Uses ordinary damage recoil, not a stomp spring, and cannot launch Mario over the seal.
            if(contactHeat>=.55f)
            {
                contactHeat=0;player.Hit(false,"The furnace firewall rejects your current subscription.");
            }
        }

        public void HitByShot(Fireball shot)
        {
            if(Destroyed||shot==null||game==null||!game.Playing||!game.hasFocus||!handledShots.Add(shot.GetInstanceID()))return;
            bool instant=shot.IsBullet;Destroy(shot.gameObject);
            Health=instant?0:Health-1;flash=.15f;
            if(Health<=0)Break();else Cue("bump");
        }

        public void SmashByMech()
        {if(!Destroyed&&game!=null&&game.Playing&&game.hasFocus){Health=0;Break();}}

        private void Break()
        {
            if(Destroyed)return;Destroyed=true;box.enabled=false;game.AddScore(id+".destroyed",200);Cue("break");
            CastleDebris.Spawn(game,transform.position,FurnaceSeal?new Color(1,.5f,.2f):new Color(.7f,.66f,.78f),16);
            gameObject.SetActive(false);Destroy(gameObject);
        }

        private void Cue(string key)
        {
            if(game.events==null||game.cameraView==null)return;
            var view=game.cameraView.WorldToViewportPoint(transform.position);
            if(view.z>0&&view.x>-.1f&&view.x<1.1f)game.events.Sound(key);
        }
    }

    // Bounded, scene-owned particles use the same pixel art and stop with the game.
    internal sealed class CastleDebris : MonoBehaviour
    {
        private SceneRoot game;
        private Vector2 velocity;
        private float age,spin;
        public static void Spawn(SceneRoot root,Vector2 center,Color tint,int count)
        {
            if(root==null||root.cameraView==null)return;
            var view=root.cameraView.WorldToViewportPoint(center);
            if(view.x<-.1f||view.x>1.1f||view.y<-.2f||view.y>1.2f)return;
            for(int i=0;i<count;i++)
            {
                var go=new GameObject("Castle debris");go.transform.SetParent(root.transform,false);go.transform.position=center;
                var sr=go.AddComponent<SpriteRenderer>();sr.sprite=CastleArt.Sprite("shard");sr.color=tint;sr.sortingOrder=11;
                var piece=go.AddComponent<CastleDebris>();piece.game=root;
                piece.velocity=new Vector2((i%5-2)*2.1f,3.5f+(i%3)*1.7f);piece.spin=i%2==0?170:-220;
            }
        }
        private void Update()
        {
            if(game==null){Destroy(gameObject);return;}
            if(!game.Playing||!game.hasFocus)return;
            float dt=Time.deltaTime;age+=dt;velocity.y-=16*dt;transform.position+=(Vector3)velocity*dt;transform.Rotate(0,0,spin*dt);
            if(age>.7f)Destroy(gameObject);
        }
    }
}
