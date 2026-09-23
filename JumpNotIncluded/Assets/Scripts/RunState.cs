using UnityEngine;

namespace JumpNotIncluded
{
    [CreateAssetMenu(menuName="Jump Not Included/Run State")]
    public class RunState : ScriptableObject
    {
        public RunModel data = new RunModel();
        public string checkpointJson;
        public Vector2 checkpointPosition = new Vector2(2, .6f);
        public Form checkpointForm = Form.Small;
        public Form carryForm = Form.Small;
        public bool restoreCheckpoint;
        public bool shopOnLoad;
        public int nextWorld = 1;
        [System.NonSerialized] private AdRotation adRotation;
        public AdCampaign NextAdCampaign()
        {if(adRotation==null)adRotation=new AdRotation();return adRotation.Next();}
        public void NewRun()
        {
            adRotation=new AdRotation();
            data = new RunModel(); checkpointJson = "";
            checkpointPosition = new Vector2(2, .6f);
            checkpointForm = carryForm = Form.Small;
            restoreCheckpoint = false; shopOnLoad = false; nextWorld = 1;
        }
        public void Capture(Vector2 safePosition, Form form)
        {
            checkpointJson = JsonUtility.ToJson(data);
            checkpointPosition = safePosition;
            checkpointForm = form == Form.Dead || form == Form.Hurt ? Form.Small : form == Form.HurtSuper ? Form.Super : form;
        }
        public void Restore()
        {
            if (string.IsNullOrEmpty(checkpointJson)) return;
            int deaths=data.deaths,ads=data.ads;
            float time=data.playTime,adTime=data.adWatchTime,inputTime=data.activeInputTime;
            data=JsonUtility.FromJson<RunModel>(checkpointJson);
            data.deaths=deaths;data.ads=ads;data.playTime=time;
            data.adWatchTime=adTime;data.activeInputTime=inputTime;
            carryForm=checkpointForm; restoreCheckpoint=true;
        }
    }
}
