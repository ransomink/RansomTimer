namespace Ransom
{
    public class TimeManager : Singleton<TimeManager>
    {
        #region Unity Callbacks
        private void OnEnable()
        {
            UpdateManager.OnFixedUpdate += OnFixedUpdate;
            UpdateManager.OnUpdate      += OnUpdate;
        }
        
        private void OnDisable()
        {
            UpdateManager.OnFixedUpdate -= OnFixedUpdate;
            UpdateManager.OnUpdate      -= OnUpdate;
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
