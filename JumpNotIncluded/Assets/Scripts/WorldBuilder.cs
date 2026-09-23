using System.Collections.Generic;
using UnityEngine;

namespace JumpNotIncluded
{
    public class WorldBuilder : MonoBehaviour
    {
        public SceneRoot game;public float length;
        private int world;
        private Transform groundRoot;
        private CompositeCollider2D groundCollider;
        private readonly List<GameObject> pipes=new List<GameObject>();
        private readonly HashSet<int> paved=new HashSet<int>();
        public int GapStart=>world==1?44:77;
        public int GapEnd=>world==1?50:79;
        public const int OpeningBossCount=10;
        public static string BossKey(int index)=>index==0?"w2.bowser":"w2.bowser."+index;
        private string PavementKey(int column)=>"w"+world+".paved."+column;
        private string PipeKey(float x)=>"w"+world+".pipe."+x.ToString(System.Globalization.CultureInfo.InvariantCulture);
        public void Init(SceneRoot root,int index)
        {
            game=root;world=index;length=world==1?96:112;
            var terrain=new GameObject("Continuous ground collision");terrain.transform.SetParent(transform,false);terrain.layer=8;
            groundRoot=terrain.transform;terrain.AddComponent<Rigidbody2D>().bodyType=RigidbodyType2D.Static;
            groundCollider=terrain.AddComponent<CompositeCollider2D>();groundCollider.geometryType=CompositeCollider2D.GeometryType.Polygons;
            groundCollider.generationType=CompositeCollider2D.GenerationType.Manual;
            Backdrop();
            Floor(0,GapStart);Floor(GapEnd,(int)length+8);
            if(world==1)BuildOne();else BuildTwo();
            for(int x=GapStart;x<GapEnd;x++)
                if(game.Run.collected.Contains(PavementKey(x))){paved.Add(x);Floor(x,x+1);}
            groundCollider.GenerateGeometry();
            Flag(length-4);
        }
        private void Update()
        {
            if(game.Playing&&game.player!=null&&game.player.transform.position.x>length-4)
                game.CompleteWorld();
        }
        public SpriteRenderer Sprite(string name,string key,Vector2 position,bool atlas=true,int order=0)
        {
            var go=new GameObject(name);go.transform.SetParent(transform,false);go.transform.position=position;
            var sr=go.AddComponent<SpriteRenderer>();sr.sprite=game.assets.Sprite(key);
            if(atlas)sr.sharedMaterial=game.assets.greenKey;
            sr.sortingOrder=order;return sr;
        }
        private void Floor(int from,int to)
        {
            var go=new GameObject("Ground "+from+"-"+to);go.transform.SetParent(groundRoot,false);go.layer=8;
            go.transform.position=new Vector3((from+to)*.5f,-1,0);
            var col=go.AddComponent<BoxCollider2D>();col.size=new Vector2(to-from,2);
            col.compositeOperation=Collider2D.CompositeOperation.Merge;
            for(int x=from;x<to;x++)for(int row=0;row<2;row++)
            {var s=Sprite("Ground tile","ground",new Vector2(x+.5f,-.5f-row));if(world==2)s.color=new Color(.65f,.75f,.85f);}
        }
        private void Backdrop()
        {
            for(int i=0;i<14;i++)
            {
                float x=i*9+3;
                var cloud=Sprite("Cloud","cloud",new Vector2(x,7.2f+(i%3)*.65f),true,-15);
                cloud.transform.localScale=Vector3.one*(i%2==0?1.2f:1.7f);
                if(world==2)cloud.color=new Color(.33f,.48f,.62f);
                string hillKey=i%3==0?"hill-large":"hill";
                float hillX=x+4,halfWidth=game.assets.Sprite(hillKey).bounds.extents.x;
                // Scenery shares the ground baseline; keep its entire base on solid terrain.
                if(hillX-halfWidth<0||hillX+halfWidth>length+8||
                    (hillX+halfWidth>GapStart&&hillX-halfWidth<GapEnd))continue;
                var hill=Sprite("Hill",hillKey,new Vector2(hillX,0),true,-10);
                if(world==2)hill.color=new Color(.4f,.6f,.6f);
            }
        }
        private void Pipe(float x,int height=2)
        {
            if(game.Run.collected.Contains(PipeKey(x)))return;
            var sr=Sprite("Pipe","pipe",new Vector2(x,height*.5f),true,1);
            sr.transform.localScale=new Vector3(1,height/2f,1);
            sr.gameObject.layer=8;var c=sr.gameObject.AddComponent<BoxCollider2D>();c.size=new Vector2(1.8f,2);
            pipes.Add(sr.gameObject);
        }
        // The final premium weapon edits a whole corridor, including traps above its muzzle.
        public void PaveVictoryLane(float origin,int facing)
        {
            if(!game.Playing||!game.Run.Owns(Product.Gatling))return;
            float from=origin-facing*.5f,to=origin+facing*14;
            float min=Mathf.Min(from,to),max=Mathf.Max(from,to);
            bool Ahead(float x,float radius=.5f)=>x+radius>=min&&x-radius<=max;
            bool newGround=false;
            for(int x=GapStart;x<GapEnd;x++)
            {
                if(x+1<min||x>max||!paved.Add(x))continue;
                game.Run.collected.Add(PavementKey(x));Floor(x,x+1);newGround=true;
            }
            if(newGround)groundCollider.GenerateGeometry();
            foreach(var block in FindObjectsByType<BlockActor>(FindObjectsSortMode.None))
                if(Ahead(block.transform.position.x))block.BreakByBullet();
            foreach(var pipe in pipes)
            {
                if(pipe==null||!Ahead(pipe.transform.position.x,.9f))continue;
                var collider=pipe.GetComponent<Collider2D>();if(!collider.enabled)continue;
                collider.enabled=false;game.Run.collected.Add(PipeKey(pipe.transform.position.x));
                game.events.Sound("break");Destroy(pipe);
            }
            foreach(var enemy in FindObjectsByType<EnemyActor>(FindObjectsSortMode.None))
                if(Ahead(enemy.transform.position.x))enemy.Defeat();
            foreach(var boss in FindObjectsByType<BossActor>(FindObjectsSortMode.None))
                if(Ahead(boss.transform.position.x,1.3f))boss.TakeDamage(BossActor.MaxHealth);
            foreach(var flame in FindObjectsByType<BossFlame>(FindObjectsSortMode.None))
                if(Ahead(flame.transform.position.x))Destroy(flame.gameObject);
            foreach(var item in FindObjectsByType<PickupActor>(FindObjectsSortMode.None))
                if(item.IsPoison&&Ahead(item.transform.position.x))item.Vaporize();
        }
        private void Block(float x,float y,bool question,bool coin,ItemKind kind=ItemKind.Coin,bool hidden=false)
        {
            string id="w"+world+".block."+x+"."+y;
            var go=new GameObject(question?"Question block":"Brick");go.transform.SetParent(transform,false);
            go.AddComponent<BlockActor>().Init(game,id,new Vector2(x,y),question,coin,kind,hidden);
        }
        public void Pickup(string id,ItemKind kind,Vector2 pos)
        {
            if(game.Run.collected.Contains(id))return;
            var go=new GameObject(kind+" "+id);go.transform.SetParent(transform,false);go.AddComponent<PickupActor>().Init(game,id,kind,pos);
        }
        private void Item(float x,float y,ItemKind kind)
        {Pickup("w"+world+".item."+x+"."+y,kind,new Vector2(x,y));}
        private void Enemy(float x)
        {
            string id="w"+world+".enemy."+x;if(game.Run.collected.Contains(id))return;
            var go=new GameObject("Goomba "+id);go.transform.SetParent(transform,false);go.AddComponent<EnemyActor>().Init(game,id,x);
        }
        private void BuildOne()
        {
            Enemy(14);Pipe(18,1);
            Block(22,3.5f,true,false,ItemKind.Mushroom);Block(21,3.5f,false,false);Block(23,3.5f,false,true);
            Block(34,3.5f,true,true);Block(37,3.5f,false,true);Block(38,3.5f,false,false);
            Item(32,.7f,ItemKind.Mushroom);Enemy(40);
            for(int i=0;i<3;i++)Block(44.5f+i,2.6f,true,true,ItemKind.Coin,true);
            for(int i=0;i<5;i++)Block(44.5f+i,4.9f,true,i!=2,i==2?ItemKind.Star:ItemKind.Coin,true);
            Item(43,2,ItemKind.Coin);Item(47,3.2f,ItemKind.Coin);Item(50.7f,2,ItemKind.Coin);
            Item(51,.7f,ItemKind.Poison);Item(53,.7f,ItemKind.Mushroom);
            Pipe(56,2);Enemy(64);
            Block(66,3.5f,true,true);Block(70,3.5f,true,false,ItemKind.Star,true);
            for(int i=0;i<3;i++)Block(82+i*2,4.5f,true,true,ItemKind.Coin,true);
            Enemy(75);Enemy(80);
            for(int i=0;i<5;i++)Item(83+i,1.8f,ItemKind.Coin);
        }
        private void BuildTwo()
        {
            for(int i=0;i<OpeningBossCount;i++)
            {
                string id=BossKey(i);if(game.Run.collected.Contains(id))continue;
                float x=8+i*2.4f;
                new GameObject("Bowser battalion "+(i+1)).AddComponent<BossActor>().Init(game,id,x,x-.7f,x+.7f,2.2f+i*.18f);
            }
            Block(21,3.5f,true,false,ItemKind.Star,true);Block(23,4.5f,true,true,ItemKind.Coin,true);
            Item(6,.7f,ItemKind.Poison);Item(10,.7f,ItemKind.Poison);
            for(int i=0;i<16;i++)
            {
                float x=36+i*2.2f;Enemy(x);Item(x+1.1f,.7f,ItemKind.Poison);
                if(i%3==0)Item(x+.55f,2,ItemKind.Coin);
            }
            Pipe(72,2);Enemy(75);Item(76,.7f,ItemKind.Poison);
            Block(82,3.5f,true,true);Block(84,3.5f,false,true);Block(86,3.5f,false,false);
            for(int i=0;i<12;i++)
            {float x=82+i*2.1f;Enemy(x);Item(x+1.05f,.7f,ItemKind.Poison);}
            Item(97,2.6f,ItemKind.Mushroom);
            for(int i=0;i<5;i++)Item(98+i,1.8f,ItemKind.Coin);
        }
        private void Flag(float x)
        {
            var pole=Sprite("Finish pole","solid",new Vector2(x,3.1f),false,1);
            pole.transform.localScale=new Vector3(.12f,6.2f,1);pole.color=new Color(.95f,.94f,.78f);
            var flag=Sprite("Finish flag","solid",new Vector2(x-.6f,5.1f),false,2);
            flag.transform.localScale=new Vector3(1.1f,.65f,1);flag.color=new Color(.45f,1,.55f);
            Sprite("Castle","castle",new Vector2(x+4,1.5f),true,0);
        }
    }
}
