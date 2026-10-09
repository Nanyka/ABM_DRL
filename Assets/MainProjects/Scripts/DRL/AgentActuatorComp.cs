using Unity.MLAgents.Actuators;
using UnityEngine;

namespace Sugarscape
{
    public class AgentActuatorComp : ActuatorComponent
    {
        private IAgentController m_Controller;
        private ActionSpec m_Action = ActionSpec.MakeDiscrete(SeekAction.ActionCount);

        public override IActuator[] CreateActuators()
        {
            m_Controller = GetComponent<IAgentController>();
            return new IActuator[] { new AgentActuator(m_Controller) };
        }

        public override ActionSpec ActionSpec { get {return m_Action;} }
    }

    public class AgentActuator : IActuator
    {
        private IAgentController m_Controller;
        private ActionSpec m_ActionSpec;

        public AgentActuator(IAgentController controller)
        {
            m_Controller = controller;
            m_ActionSpec = ActionSpec.MakeDiscrete(SeekAction.ActionCount);
        }
        
        public void OnActionReceived(ActionBuffers actionBuffers)
        {
            m_Controller.Move(actionBuffers.DiscreteActions[0]);
        }

        public void Heuristic(in ActionBuffers actionBuffersOut)
        {
            var discreteActions = actionBuffersOut.DiscreteActions;
            discreteActions[0] = Random.Range(0, 5);
            // discreteActions[0] = m_Controller.GetAction();
        }

        public void WriteDiscreteActionMask(IDiscreteActionMask actionMask) { }

        public void ResetData() { }

        public ActionSpec ActionSpec { get{return m_ActionSpec;} }
        public string Name { get{return "AgentActuator";} }
    }
}