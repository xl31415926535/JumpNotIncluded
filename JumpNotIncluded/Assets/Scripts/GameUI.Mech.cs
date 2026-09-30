using UnityEngine;

namespace JumpNotIncluded
{
    public partial class GameUI
    {
        private void MechShowcase()
        {
            CommerceHeader("SL CHEATER / ARSENAL");
            var violet=new Color(.67f,.64f,1);
            Gradient(176,184,484,534,new Color(.13f,.12f,.28f),new Color(.025f,.05f,.12f));
            for(int i=0;i<34;i++)
            {
                float starX=192+(i*83%450),starY=204+(i*137%474);
                float alpha=.23f+.25f*(1+Mathf.Sin(Time.unscaledTime*.8f+i));
                Box(starX,starY,i%5==0?3:1,i%5==0?3:1,new Color(.65f,.8f,1,alpha));
            }
            Text(196,201,444,29,"SL CHEATER x JUMP NOT INCLUDED",16,violet,true,TextAnchor.MiddleCenter);
            Text(196,240,444,49,"WAR GOD REPLICA",31,paper,true,TextAnchor.MiddleCenter);
            if(game.assets.mechPortrait!=null)
                GUI.DrawTexture(new Rect(279,290,279,370),game.assets.mechPortrait,ScaleMode.ScaleToFit,true);
            Text(196,666,444,30,"STARFIELD CAMOUFLAGE",20,cyan,true,TextAnchor.MiddleCenter);

            Text(693,188,706,29,"MOON KILLER EDITION / ORBITAL ASSAULT",16,violet,true);
            Text(690,229,710,54,"Why jump when you can ascend?",34,paper,true);
            Text(693,296,700,70,"SL CHEATER's legendary War God descends from orbit. Your consciousness becomes light and enters its core. Ascend to 1% light speed.",20,muted);
            MechFeature(693,388,"SOVEREIGN ARMOR","Every original monster, including Bowser, loses the privilege of hurting you.",cyan);
            MechFeature(693,467,"ORBITAL THRUSTERS","Hold SPACE to rise. Release to hover. S / DOWN descends. Even the abyss cannot cancel your existence.",violet);
            MechFeature(693,546,"JUDGMENT, AUTOMATED","Hold J. The laser locks on to enemies and delivers instant deletion. Your aim is now somebody else's problem.",gold);

            bool owned=game.Run.Owns(Product.Mech),affordable=game.Run.wallet>=RunModel.MechCost;
            Text(694,642,693,29,owned?(game.Run.mechDeployed?"SYNCHRONIZED / YOUR WAR GOD IS EQUIPPED":"PURCHASED / ORBITAL DELIVERY AWAITS"):
                "ONE-TIME WALLET PAYMENT / S$79.99 / THIS RUN",18,owned?cyan:gold,true);
            Text(694,680,693,33,owned?"SPACE UP / RELEASE HOVER / S DOWN / J AUTO-LOCK":
                "Wallet: "+Money(game.Run.wallet)+(affordable?"  /  After purchase: "+Money(game.Run.wallet-RunModel.MechCost):"  /  Earn SGD from ads below."),17,muted);
            Button(176,735,272,48,"BACK TO UPGRADES",game.CloseMechShowcase,false,true,true);
            Button(464,735,298,48,"WATCH ADS / EARN SGD",()=>game.StartAd(AdKind.Cash),false,game.Run.wallet<RunModel.WalletLimit);
            Button(782,735,632,48,owned?(game.ReviveRequired?"REVIVE - 2 SECOND AD":"DEPLOY WAR GOD"):
                affordable?"BUY WAR GOD / S$79.99":"S$79.99 / MORE WALLET CREDIT NEEDED",()=>
                {
                    if(!owned){game.BuyMech();return;}
                    if(game.ReviveRequired)game.StartAd(AdKind.Revive);
                    else{game.CloseMechShowcase();game.CloseOverlay();}
                },true,owned||affordable);
            Text(176,790,1238,22,"Licensed for this dimension. Upgrade lasts this run. All payments are simulated.",12,muted,false,TextAnchor.MiddleCenter);
        }
        private void MechFeature(float x,float y,string title,string description,Color accent)
        {
            Box(x,y+2,3,61,accent);Text(x+16,y,671,27,title,16,accent,true);
            Text(x+16,y+30,671,48,description,17,paper);
        }
    }
}
