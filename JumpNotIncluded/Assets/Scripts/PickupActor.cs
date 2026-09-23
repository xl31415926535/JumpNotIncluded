using UnityEngine;

namespace JumpNotIncluded
{
    public class PickupActor : MonoBehaviour
    {
        public SceneRoot game;public string id;public ItemKind kind;
        private SpriteRenderer sprite;private float age,baseY;private bool taken,flowerOfferShown;
        public void Init(SceneRoot root,string key,ItemKind type,Vector2 position)
        {
            game=root;id=key;kind=type;transform.position=position;baseY=position.y;
            sprite=gameObject.AddComponent<SpriteRenderer>();sprite.sharedMaterial=game.assets.greenKey;sprite.sortingOrder=4;
            var collider=gameObject.AddComponent<BoxCollider2D>();collider.size=Vector2.one*.75f;collider.isTrigger=true;
            UpdateLook();
        }
        public bool IsPoison=>kind==ItemKind.Poison&&!game.Run.Owns(Product.Purify)&&!(game.player!=null&&game.player.buffs.Value==Buff.VIP);
        public void Vaporize()
        {
            if(taken||!game.Playing)return;
            taken=true;GetComponent<Collider2D>().enabled=false;
            if(!game.Run.collected.Contains(id))game.Run.collected.Add(id);
            Destroy(gameObject);
        }
        private void Update()
        {
            if(game.Playing){age+=Time.deltaTime;transform.position=new Vector3(transform.position.x,baseY+Mathf.Sin(age*3)*.08f,0);}
            UpdateLook();
        }
        private void UpdateLook()
        {
            sprite.sprite=game.assets.Sprite(kind==ItemKind.Coin?"coin":kind==ItemKind.Flower?"flower":kind==ItemKind.Star?"star":"mushroom");
            sprite.color=IsPoison?new Color(.65f,.4f,1):Color.white;
        }
        private void OnTriggerEnter2D(Collider2D other){Take(other);}
        private void OnTriggerStay2D(Collider2D other){Take(other);}
        private void Take(Collider2D other)
        {
            if(taken||!game.Playing||age<.3f)return;
            var p=other.GetComponent<PlayerMotor>();if(p==null)return;
            if(!game.Run.CanCollect(kind))
            {
                if(!flowerOfferShown)
                {flowerOfferShown=true;game.events.Sound("error");}
                return;
            }
            if(IsPoison&&p.buffs.Value==Buff.None){p.Hit(true);return;}
            taken=true;
            if(kind==ItemKind.Mushroom||kind==ItemKind.Poison&&!IsPoison)p.PowerUp("mushroom");
            if(kind==ItemKind.Flower)p.PowerUp("flower");
            if(kind==ItemKind.Star&&p.buffs.Value!=Buff.VIP)p.buffs.Signal("star");
            if(kind==ItemKind.Coin){game.AddCoin(id);game.events.Sound("coin");}
            else game.AddScore(id,200);
            Destroy(gameObject);
        }
    }
}
