namespace Ransom
{
    public class TimeManager : Singleton<TimeManager>
    {
        #region Unity Callbacks
        private void FixedUpdate()
        {
            Time.Instance.OnFixedUpdate();
        }
    
        private void Update()
        {
            Time.Instance.OnUpdate();
            Timer.OnUpdate();
        }
        #endregion Unity Callbacks
    }
}
