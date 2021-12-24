namespace Ransom
{
    public class TimeManager : Singleton<TimeManager>
    {
        #region Unity Callbacks
        private void OnEnable()
        {
            UpdateManager.OnUpdate      += OnUpdate;
            UpdateManager.OnFixedUpdate += OnFixedUpdate;
        }
        
        private void OnDisable()
        {
            UpdateManager.OnUpdate      -= OnUpdate;
            UpdateManager.OnFixedUpdate -= OnFixedUpdate;
        }
        
        private void OnFixedUpdate()
        {
            StaticTime.OnFixedUpdate();
            // Time.Instance.OnFixedUpdate();
        }
    
        private void OnUpdate()
        {
            StaticTime.OnUpdate();
            // Time.Instance.OnUpdate();
            Timer.OnUpdate();
        }
        #endregion Unity Callbacks
    }
}
