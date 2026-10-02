using UnityEngine;

namespace JumpNotIncluded
{
    // Deliberately immune to bullets: the Gatling buys crowd control, not a bridge.
    public sealed class CastlePlatform : MonoBehaviour
    {
        private SceneRoot game;private BoxCollider2D box;public string id;
        public void Init(SceneRoot root,string key,Vector2 top,float width,bool suspended)
        {
            game=root;id=key;transform.position=top;gameObject.layer=8;
            if(game.Run.collected.Contains(id+".smashed")){Destroy(gameObject);return;}
            float height=suspended?.65f:top.y+2.2f;
            box=gameObject.AddComponent<BoxCollider2D>();box.size=new Vector2(width,height);box.offset=Vector2.down*height*.5f;
            if(suspended)
            {
                // One-way ledges keep the low route's jump arcs clear underneath.
                var effector=gameObject.AddComponent<PlatformEffector2D>();
                effector.useOneWay=true;effector.useOneWayGrouping=true;effector.surfaceArc=160;
                box.usedByEffector=true;
            }
            for(int row=0;row<Mathf.CeilToInt(height);row++)
            {
                var go=new GameObject("Basalt masonry");go.transform.SetParent(transform,false);
                float h=Mathf.Min(1,height-row);go.transform.localPosition=new Vector3(0,-row-h*.5f,0);
                go.transform.localScale=new Vector3(width,h,1);
                var s=go.AddComponent<SpriteRenderer>();s.sprite=CastleArt.Sprite("brick");s.color=suspended?new Color(.63f,.50f,.59f):new Color(.53f,.41f,.46f);s.sortingOrder=2;
            }
            var rim=new GameObject("Lit stone rim");rim.transform.SetParent(transform,false);rim.transform.localPosition=Vector3.down*.045f;
            rim.transform.localScale=new Vector3(width,.09f,1);var light=rim.AddComponent<SpriteRenderer>();light.sprite=game.assets.solid;
            light.color=suspended?new Color(1,.75f,.37f):new Color(1,.32f,.09f);light.sortingOrder=3;
        }
        public void Smash()
        {
            if(!game.Playing||box==null||!box.enabled)return;
            box.enabled=false;game.Run.collected.Add(id+".smashed");game.events.Sound("break");Destroy(gameObject);
        }
    }
}
