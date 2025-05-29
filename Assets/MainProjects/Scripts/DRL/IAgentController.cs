using Unity.MLAgents.Actuators;

namespace Sugarscape
{
    public interface IAgentController
    {
        public void Init(int agentId, int x, int y);
        public void Move(int action);
        public void Eat();
        public void MayBeDie();
        public int GetAction();
    }
}