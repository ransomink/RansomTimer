using UnityEngine;

namespace Ransom
{
    public sealed class TimeManager : Singleton<TimeManager>
    {
        #region Properties
        [SerializeField] private SO_TimerManager TimerManager = default;
        #endregion Properties

        #region Unity Callbacks
        private void OnEnable()
        {
            UpdateDispatcher.OnFixedUpdate += OnFixedUpdate;
            UpdateDispatcher.OnUpdate      += OnUpdate;
        }
        
        private void OnDisable()
        {
            UpdateDispatcher.OnFixedUpdate -= OnFixedUpdate;
            UpdateDispatcher.OnUpdate      -= OnUpdate;
        }
        
        private void OnFixedUpdate()
        {
            // Time.Instance.OnFixedUpdate();
            StaticTime.OnFixedUpdate();
        }
    
        private void OnUpdate()
        {
            // Time.Instance.OnUpdate();
            StaticTime.OnUpdate();
            TimerManager.OnUpdate();
        }
        #endregion Unity Callbacks
    }
}
