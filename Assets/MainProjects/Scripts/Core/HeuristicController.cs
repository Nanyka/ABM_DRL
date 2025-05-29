using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Sugarscape
{
    public class HeuristicController : MonoBehaviour
    {
        [SerializeField] private VoidChannel OnExecuteAction;
        [SerializeField] private IntStorage ActionStorage;

        private SugarscrapeInput controls;

        private void Awake()
        {
            controls = new SugarscrapeInput();

            // Subscribe to the performed callback
            controls.HeuristicControl.Space.performed += OnSpace;
            controls.HeuristicControl.Up.performed += OnUp;
            controls.HeuristicControl.Down.performed += OnDown;
            controls.HeuristicControl.Left.performed += OnLeft;
            controls.HeuristicControl.Right.performed += OnRight;
        }

        private void OnEnable()
        {
            controls.HeuristicControl.Enable();
        }

        private void OnDisable()
        {
            controls.HeuristicControl.Disable();
        }

        private void OnSpace(InputAction.CallbackContext context)
        {
            ActionStorage.SetValue(0);
            OnExecuteAction.ExecuteChannel();
        }
        
        private void OnUp(InputAction.CallbackContext context)
        {
            ActionStorage.SetValue(3);
            OnExecuteAction.ExecuteChannel();
        }
        
        private void OnDown(InputAction.CallbackContext context)
        {
            ActionStorage.SetValue(4);

            OnExecuteAction.ExecuteChannel();
        }
        
        private void OnLeft(InputAction.CallbackContext context)
        {
            ActionStorage.SetValue(1);

            OnExecuteAction.ExecuteChannel();
        }
        
        private void OnRight(InputAction.CallbackContext context)
        {
            ActionStorage.SetValue(2);
            OnExecuteAction.ExecuteChannel();
        }

        private void OnDestroy()
        {
            // Clean up subscription
            controls.HeuristicControl.Space.performed -= OnSpace;
            controls.HeuristicControl.Up.performed -= OnUp;
            controls.HeuristicControl.Down.performed -= OnDown;
            controls.HeuristicControl.Left.performed -= OnLeft;
            controls.HeuristicControl.Right.performed -= OnRight;
        }
    }
}