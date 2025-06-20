using Unity.MLAgents.Actuators;
using UnityEngine;

namespace Sugarscape
{
    public interface IAgentController
    {
        public void Init(int agentId, int x, int y, bool isShowId = false);
        public void Move(int action);
        public void Eat();
        public void MayBeDie();
        public void AskForActions();
        public void UpdateState();
        public void AgentReset();
        public (int,int) GetPosition();
        public int GetAgentID();
        public int GetVision();
        public bool IsPerfectInfo();
        public bool IsAlive();
        public void ChangeSugar(int sugarAmount);
        public void ChangeSpice(int spiceAmount);
        public ITradeComp GetTradeComp();
        public int RemainSugar();
        public int RemainSpice();
        public float CurrentMrs();
        public float ObserveSugarStarve();
        public float ObserveSpiceStarve();
        public float SugarSpiceDistance();
        public bool IsHardCodeAgent();
        public GameObject GetGameObject();
    }
}