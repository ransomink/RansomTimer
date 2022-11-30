namespace Ransom
{
    public class TimeManager : Singleton<TimeManager>
    {
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
            Timer.OnUpdate();
        }
        #endregion Unity Callbacks
    }
}
