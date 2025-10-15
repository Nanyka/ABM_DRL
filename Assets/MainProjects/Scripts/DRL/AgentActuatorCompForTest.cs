using System.Threading.Tasks;
using Unity.MLAgents.Actuators;
using Unity.Sentis;
using UnityEngine;

namespace Sugarscape
{
    public class AgentActuatorCompForTest : ActuatorComponent
    {
        private IAgentController m_Controller;
        private ActionSpec m_Action = ActionSpec.MakeDiscrete(5);

        public override IActuator[] CreateActuators()
        {
            m_Controller = GetComponent<IAgentController>();
            return new IActuator[] { new AgentActuatorForTest(m_Controller) };
        }

        public override ActionSpec ActionSpec { get {return m_Action;} }
    }
    
    public class AgentActuatorForTest : IActuator
    {
        private IAgentController m_Controller;
        private ActionSpec m_ActionSpec;
        private readonly IObservationProvider _obsProvider;
        private readonly int _numActions = 5;
        private Tensor<float> _inputTensorVisual;
        private Tensor<float> _inputTensorFloat;
        private Tensor<float> _inputTensorMask = new Tensor<float>(new TensorShape(1, 5), new float[5]);
        private Tensor<float> _outputTensor;
        private int actionIndex = 0;

        public AgentActuatorForTest(IAgentController controller)
        {
            m_Controller = controller;
            m_ActionSpec = ActionSpec.MakeDiscrete(5);
            _obsProvider = controller.GetObservationProvider();
        }
        
        public void OnActionReceived(ActionBuffers actionBuffers)
        {
            m_Controller.Move(actionBuffers.DiscreteActions[0]);
        }

        public void Heuristic(in ActionBuffers actionBuffersOut)
        {
            var visualObs = _obsProvider?.GetVisualObs();
            var floatObs = _obsProvider?.GetFloatObs();
            if (_obsProvider != null)
            {
                GetPredictAction(visualObs, floatObs, _obsProvider.GetWorker());
                var discreteActions = actionBuffersOut.DiscreteActions;
                discreteActions[0] = actionIndex;
            }
        }

        public void WriteDiscreteActionMask(IDiscreteActionMask actionMask) { }

        public void ResetData() { }

        public ActionSpec ActionSpec { get{return m_ActionSpec;} }
        public string Name { get{return "AgentActuator";} }
        
        private async void GetPredictAction(float[] visualObs, float[] floatObs, Worker worker)
        {
            _inputTensorVisual = new Tensor<float>(new TensorShape(1, visualObs.Length), visualObs);
            _inputTensorFloat = new Tensor<float>(new TensorShape(1, floatObs.Length), floatObs);

            _inputTensorVisual?.Dispose();
            _inputTensorFloat?.Dispose();
            _inputTensorMask?.Dispose();
            worker.Schedule(_inputTensorFloat,_inputTensorVisual,_inputTensorMask);
            var outputTensor = worker.PeekOutput() as Tensor<int>;
            var result = await outputTensor.ReadbackAndCloneAsync();
            actionIndex = result[0];
            outputTensor.Dispose();
            // Debug.Log($"{outputTensor.count}, {outputTensor.shape}, {outputTensor.dataType}, {outputTensor.ToString()}");
        }
    }
}