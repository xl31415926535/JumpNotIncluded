using UnityEngine;

namespace JumpNotIncluded
{
    public partial class WorldBuilder
    {
        public bool IsCastle=>world==3;
        public const float CastleCeiling=11.25f;
        public const float UpperRouteHeight=6.4f;
        // Lower route: near-maximum single jumps. The separate high route needs the Wings.
        public static readonly Vector3[] PrecisionPlatforms={
            new Vector3(28.7f,.5f,1.25f),new Vector3(35.25f,.9f,1.1f),new Vector3(41.7f,.35f,1.3f),
            new Vector3(64,.25f,1.15f),new Vector3(70.4f,1.25f,1.25f),
            new Vector3(91.8f,.6f,1.2f),new Vector3(98.1f,1.25f,1.1f),new Vector3(105.1f,.3f,1.35f),
            new Vector3(129.8f,.8f,1.2f),new Vector3(135.3f,.3f,1.25f)};
        private void BuildThree()
        {
            CastleBackdrop();
            foreach(var deck in new[]{new Vector2(0,23),new Vector2(47,58),new Vector2(76,86),new Vector2(112,124),new Vector2(139,164)})
                Floor((int)deck.x,(int)deck.y);
            foreach(Transform tile in transform)
                if(tile.name=="Ground tile"){var s=tile.GetComponent<SpriteRenderer>();s.sprite=CastleArt.Sprite("brick");s.color=new Color(.50f,.46f,.54f);}
            // A tall, un-stompable seal has no free item, enemy or ledge before it.
            // Jump reaches ~4.6 units above its launch; even Super Mario cannot crest 7.5.
            Barrier("entry",new Vector2(16,3.25f),new Vector2(1.5f,8.5f),true);
            Barrier("furnace",new Vector2(56,2.5f),new Vector2(1.5f,7),true);
            Barrier("throne",new Vector2(141,3.25f),new Vector2(2,8.5f),true);
            int i=0;
            foreach(var p in PrecisionPlatforms)Platform("lower."+i++,p.x,p.y,p.z,false);
            i=0;
            foreach(float x in new[]{32f,40f,65f,73f,93f,102f,132.5f})
                Platform("upper."+i++,x,UpperRouteHeight,1.8f,true);
            // Human terrain remains after Gatling fire. Mech contact pulverizes it instead.
            Pipe(82,3);Pipe(120,2);
            foreach(var pit in new[]{new Vector2(23,47),new Vector2(58,76),new Vector2(86,112),new Vector2(124,139)})
                Hazard("lava."+pit.x,CastleHazardKind.Lava,new Vector2((pit.x+pit.y)*.5f,-1.15f),new Vector2(pit.y-pit.x,1.6f));
            Hazard("chain.1",CastleHazardKind.FireBar,new Vector2(29,-.3f),new Vector2(3.6f,1),.2f);
            Hazard("chain.2",CastleHazardKind.FireBar,new Vector2(41,.1f),new Vector2(4.0f,1),1.0f);
            Hazard("chain.3",CastleHazardKind.FireBar,new Vector2(65,.3f),new Vector2(4.2f,1),.7f);
            Hazard("chain.4",CastleHazardKind.FireBar,new Vector2(94,0),new Vector2(4.4f,1),1.3f);
            Hazard("chain.5",CastleHazardKind.FireBar,new Vector2(105,.1f),new Vector2(3.8f,1),.1f);
            Hazard("chain.6",CastleHazardKind.FireBar,new Vector2(134,-.2f),new Vector2(4.3f,1),.9f);
            Hazard("crusher.1",CastleHazardKind.Crusher,new Vector2(36,8.2f),new Vector2(2.7f,5.7f),.5f);
            Hazard("crusher.2",CastleHazardKind.Crusher,new Vector2(72,9.6f),new Vector2(3.0f,5.8f),1.3f);
            Hazard("crusher.3",CastleHazardKind.Crusher,new Vector2(100,9.6f),new Vector2(2.8f,5.9f),.1f);
            Hazard("crusher.4",CastleHazardKind.Crusher,new Vector2(137,8.2f),new Vector2(2.8f,6.3f),.8f);
            Hazard("geyser.1",CastleHazardKind.LavaJet,new Vector2(25.5f,-.5f),new Vector2(.8f,7),0);
            Hazard("geyser.2",CastleHazardKind.LavaJet,new Vector2(61,-.5f),new Vector2(.8f,7.7f),1.0f);
            Hazard("geyser.3",CastleHazardKind.LavaJet,new Vector2(89,-.5f),new Vector2(.8f,8),.6f);
            Hazard("geyser.4",CastleHazardKind.LavaJet,new Vector2(127,-.5f),new Vector2(.8f,7),1.4f);
            CastleBoss("w3.bowser.1",53,52,54,.8f);
            CastleBoss("w3.bowser.2",79,78,80,1.1f);
            CastleBoss("w3.bowser.3",118,117,119,.7f);
            CastleBoss("w3.bowser.4",146,145,147,.4f);
            foreach(float x in new[]{50f,77f,114f,143f,149f})Enemy(x);
            // Coins trace both routes; they are rewards rather than free traversal power-ups.
            foreach(var p in PrecisionPlatforms)Item(p.x,p.y+2.5f,ItemKind.Coin);
            foreach(float x in new[]{32f,40f,65f,73f,93f,102f,132.5f})Item(x,UpperRouteHeight+1.5f,ItemKind.Coin);
        }
        private void Platform(string key,float x,float top,float width,bool high)
        {
            var go=new GameObject("Castle pillar / "+key);go.transform.SetParent(transform,false);
            go.AddComponent<CastlePlatform>().Init(game,"w3.platform."+key,new Vector2(x,top),width,high);
        }
        private void Barrier(string key,Vector2 at,Vector2 size,bool seal)
        {var go=new GameObject("Furnace seal / "+key);go.transform.SetParent(transform,false);go.AddComponent<CastleBarrier>().Init(game,"w3.seal."+key,at,size,seal);}
        private void Hazard(string key,CastleHazardKind kind,Vector2 at,Vector2 size,float phase=0)
        {var go=new GameObject("Castle / "+key);go.transform.SetParent(transform,false);go.AddComponent<CastleHazard>().Init(game,"w3."+key,kind,at,size,phase);}
        private void CastleBoss(string id,float x,float left,float right,float delay)
        {if(!game.Run.collected.Contains(id))new GameObject("Furnace Bowser").AddComponent<BossActor>().Init(game,id,x,left,right,delay);}
        private void CastleBackdrop()
        {
            var backdrop=new GameObject("Obsidian fortress backdrop");backdrop.transform.SetParent(transform,false);
            for(int x=0;x<164;x+=4)
            {
                // Repeating buttresses and barred windows establish scale behind the hazards.
                Decoration(backdrop.transform,"buttress",CastleArt.Sprite("brick"),new Vector2(x,5),new Vector2(1.3f,12),new Color(.19f,.12f,.20f),-12);
                Decoration(backdrop.transform,"arch",CastleArt.Sprite("window"),new Vector2(x+2,7.5f),new Vector2(1.1f,2.2f),new Color(.8f,.32f,.16f),-11);
                Decoration(backdrop.transform,"wall band",CastleArt.Sprite("brick"),new Vector2(x+2,10),new Vector2(4,1),new Color(.24f,.18f,.26f),-10);
            }
            // The ceiling blocks out-of-map skips; it is well above the actual upper route.
            var roof=new GameObject("Castle ceiling");roof.transform.SetParent(transform,false);roof.layer=8;
            roof.transform.position=new Vector3(78,CastleCeiling+.5f,0);roof.AddComponent<BoxCollider2D>().size=new Vector2(172,1);
            Decoration(roof.transform,"roof",CastleArt.Sprite("brick"),roof.transform.position,new Vector2(172,1),new Color(.45f,.34f,.42f),1);
            foreach(float x in new[]{7f,51f,115f,148f})
                Decoration(backdrop.transform,"royal banner",CastleArt.Sprite("banner"),new Vector2(x,8.4f),new Vector2(1.5f,3),Color.white,-8);
        }
        private static void Decoration(Transform parent,string name,Sprite sprite,Vector2 at,Vector2 size,Color tint,int order)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=at;
            var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.color=tint;sr.sortingOrder=order;
            go.transform.localScale=new Vector3(size.x/sprite.bounds.size.x,size.y/sprite.bounds.size.y,1);
        }
        // Sweep before velocity is integrated, so a chassis never needs to squeeze through a wall.
        public void ClearMechObstacles(Vector2 position,Vector2 next)
        {
            if(!IsCastle||!game.Playing||game.player==null||!game.player.MechActive)return;
            Vector2 center=(position+next)*.5f;
            Vector2 size=new Vector2(Mathf.Abs(next.x-position.x)+1.65f,Mathf.Abs(next.y-position.y)+2.55f);
            foreach(var col in Physics2D.OverlapBoxAll(center,size,0))
            {
                var wall=col.GetComponentInParent<CastleBarrier>();if(wall!=null){wall.SmashByMech();continue;}
                var pillar=col.GetComponentInParent<CastlePlatform>();if(pillar!=null){pillar.Smash();continue;}
                var hazard=col.GetComponentInParent<CastleHazard>();if(hazard!=null){hazard.BreakByWeapon(true);continue;}
                var enemy=col.GetComponent<EnemyActor>();if(enemy!=null){enemy.Defeat();continue;}
                var boss=col.GetComponent<BossActor>();if(boss!=null){boss.TakeDamage(BossActor.MaxHealth);continue;}
                var flame=col.GetComponent<BossFlame>();if(flame!=null){Destroy(flame.gameObject);continue;}
                if(col.gameObject.name=="Pipe"&&col.enabled)
                {col.enabled=false;Destroy(col.gameObject);game.events.Sound("break");}
            }
        }
    }
}
