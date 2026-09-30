using UnityEngine;

namespace JumpNotIncluded
{
    public partial class SceneRoot
    {
        public bool mechShowcaseOpen {get;private set;}
        public MechArrival mechArrival {get;private set;}
        private bool finishingMechArrival;

        private void BeginMechArrival()
        {
            if(mechArrival!=null||ReviveRequired)return;
            SetMode(ScreenMode.Deployment);toastUntil=0;
            player.body.linearVelocity=Vector2.zero;player.body.simulated=false;
            var go=new GameObject("Unskippable orbital delivery");go.transform.SetParent(transform,false);
            mechArrival=go.AddComponent<MechArrival>();
            mechArrival.Init(this,CompleteMechArrival);
        }
        private void CompleteMechArrival()
        {
            if(!Deploying||mechArrival==null||!mechArrival.Finished)return;
            Vector2 position=mechArrival.LandingPosition;
            Run.mechDeployed=true;player.RefreshEquipment();
            player.body.position=position;player.transform.position=position;
            player.body.linearVelocity=Vector2.zero;player.body.simulated=true;
            SaveCheckpoint(session.checkpointPosition);
            finishingMechArrival=true;
            try{SetMode(ScreenMode.Playing);}finally{finishingMechArrival=false;}
        }

        public void OpenMechShowcase()
        {
            if(mode!=ScreenMode.Shop||rechargeOpen)return;
            mechShowcaseOpen=true;toastUntil=0;ui?.ResetFocus();
        }
        public void CloseMechShowcase()
        {mechShowcaseOpen=false;toastUntil=0;ui?.ResetFocus();}

        public bool BuyMech()
        {
            if(mode!=ScreenMode.Shop||rechargeOpen)return false;
            if(!Run.BuyMech())
            {
                events.Sound("error");
                Toast(Run.Owns(Product.Mech)?"This run already owns a War God replica.":
                    "The War God accepts S$79.99 from your SGD wallet. Earn SGD by watching ads.",4);
                return false;
            }
            player?.RefreshEquipment();events.Sound("powerup");
            SaveCheckpoint(new Vector2(session.checkpointPosition.x,player!=null&&player.Big?1.02f:.55f));
            Toast("ORBITAL DELIVERY CONFIRMED. Your consciousness will become light. Deployment follows revival.",5);
            // Purchase queues the arrival; revival and consciousness uplink must finish before control.
            return true;
        }
    }
}
