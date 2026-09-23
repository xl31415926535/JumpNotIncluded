using UnityEngine;

namespace JumpNotIncluded
{
    public class BlockActor : MonoBehaviour
    {
        public SceneRoot game;public string id;public bool question,hasCoin,spent,hidden,revealed;
        public ItemKind contents;
        private SpriteRenderer sprite,popCoin,hint;private SpriteRenderer[] outline;
        private BoxCollider2D box;private float bumpTime=-1,age;private Vector3 start;
        public bool GuideVisible=>hidden&&!revealed&&game.Run.Owns(Product.MasterGuide);
        public void Init(SceneRoot root,string key,Vector2 position,bool isQuestion,bool coin,ItemKind item,bool concealed=false)
        {
            game=root;id=key;question=isQuestion;hasCoin=coin;contents=item;hidden=concealed;start=position;transform.position=position;
            var visual=new GameObject("Sprite");visual.transform.SetParent(transform,false);
            sprite=visual.AddComponent<SpriteRenderer>();sprite.sharedMaterial=game.assets.greenKey;sprite.sortingOrder=3;
            box=gameObject.AddComponent<BoxCollider2D>();box.size=Vector2.one;
            spent=game.Run.collected.Contains(id);revealed=!hidden||spent;
            if(game.Run.collected.Contains(id+".broken")||spent&&!question&&!hasCoin)
            {if(question&&contents!=ItemKind.Coin)SpawnContents();Destroy(gameObject);return;}
            box.isTrigger=!revealed;gameObject.layer=revealed?8:10;
            sprite.sprite=game.assets.Sprite(spent?"spent":question?"question":"brick");
            if(hidden)
            {
                outline=new SpriteRenderer[4];
                for(int i=0;i<4;i++)
                {
                    var edge=new GameObject("Guide outline");edge.transform.SetParent(transform,false);
                    outline[i]=edge.AddComponent<SpriteRenderer>();outline[i].sprite=game.assets.solid;outline[i].sortingOrder=6;
                    outline[i].color=new Color(1,.87f,.32f,.9f);
                    edge.transform.localPosition=i<2?new Vector3(0,(i==0?1:-1)*.52f,0):new Vector3((i==2?1:-1)*.52f,0,0);
                    edge.transform.localScale=i<2?new Vector3(1.06f,.045f,1):new Vector3(.045f,1.06f,1);
                }
                var icon=new GameObject("Hidden reward guide");icon.transform.SetParent(transform,false);icon.transform.localScale=Vector3.one*.62f;
                hint=icon.AddComponent<SpriteRenderer>();hint.sprite=game.assets.Sprite(contents==ItemKind.Star?"star":contents==ItemKind.Coin?"coin":"mushroom");
                hint.sharedMaterial=game.assets.greenKey;hint.sortingOrder=7;
            }
            UpdateLook();if(spent&&question&&contents!=ItemKind.Coin)SpawnContents();
        }
        private void UpdateLook()
        {
            sprite.enabled=revealed;
            if(outline!=null){foreach(var edge in outline)edge.enabled=GuideVisible;hint.enabled=GuideVisible;}
        }
        private void Update()
        {
            UpdateLook();if(!game.Playing)return;age+=Time.deltaTime;
            if(bumpTime>=0)
            {
                bumpTime+=Time.deltaTime;float offset=Mathf.Sin(Mathf.Clamp01(bumpTime/.24f)*Mathf.PI)*.18f;
                sprite.transform.localPosition=Vector3.up*offset;
                if(popCoin!=null){popCoin.transform.position=start+Vector3.up*(.65f+Mathf.Sin(Mathf.Clamp01(bumpTime/.65f)*Mathf.PI)*1.3f);if(bumpTime>.65f)Destroy(popCoin.gameObject);}
                if(bumpTime>.7f)bumpTime=-1;
            }
            sprite.color=question&&!spent?Color.Lerp(Color.white,new Color(1,.75f,.35f),.2f+Mathf.Sin(age*4)*.2f):Color.white;
        }
        private void OnTriggerEnter2D(Collider2D other){TryReveal(other);}
        private void OnTriggerStay2D(Collider2D other){TryReveal(other);}
        private void TryReveal(Collider2D other)
        {
            if(revealed||!game.Playing)return;
            var p=other.GetComponent<PlayerMotor>();if(p==null||p.body.linearVelocity.y<=0)return;
            float bottom=box.bounds.min.y,head=p.box.bounds.max.y;
            if(p.box.bounds.center.y>=bottom||head-p.body.linearVelocity.y*Time.fixedDeltaTime>bottom+.12f)return;
            revealed=true;gameObject.layer=8;box.isTrigger=false;
            p.body.position=new Vector2(p.body.position.x,bottom-p.box.size.y*.5f-.025f);
            p.body.linearVelocity=new Vector2(p.body.linearVelocity.x,0);
            HitFromBelow(p);UpdateLook();
        }
        private void OnCollisionEnter2D(Collision2D collision)
        {
            var p=collision.collider.GetComponent<PlayerMotor>();if(p==null||spent||!game.Playing)return;
            if(p.box.bounds.center.y>=transform.position.y)return;
            foreach(var contact in collision.contacts)
                if(contact.normal.y>.5f&&contact.point.y<transform.position.y-.4f){HitFromBelow(p);return;}
        }
        private void HitFromBelow(PlayerMotor p)
        {
            if(spent)return;bumpTime=0;game.events.Sound("bump");
            if(!question&&!hasCoin)
            {if(p.Big){game.AddScore(id,50);game.events.Sound("break");Destroy(gameObject);}return;}
            AwardContents();
        }
        private void AwardContents()
        {
            if(spent)return;spent=true;revealed=true;game.Run.collected.Add(id);sprite.sprite=game.assets.Sprite("spent");
            if(contents!=ItemKind.Coin&&question){SpawnContents();game.events.Sound("appear");}
            else if(hasCoin)
            {
                game.AddCoin(id+".coin");game.events.Sound("coin");
                var go=new GameObject("Coin animation");popCoin=go.AddComponent<SpriteRenderer>();popCoin.sprite=game.assets.Sprite("coin");
                popCoin.sharedMaterial=game.assets.greenKey;popCoin.sortingOrder=6;bumpTime=0;
            }
        }
        public void BreakByBullet()
        {
            if(!game.Playing||game.Run.collected.Contains(id+".broken"))return;
            AwardContents();if(popCoin!=null)Destroy(popCoin.gameObject);
            box.enabled=false;
            game.AddScore(id+".broken",50);game.events.Sound("break");Destroy(gameObject);
        }
        private void SpawnContents()
        {game.level.Pickup(id+".item",contents,new Vector2(start.x,start.y+1.05f));}
    }
}
