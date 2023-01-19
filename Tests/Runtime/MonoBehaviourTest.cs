using UnityEngine;

namespace Ransom.Tests
{
    public class MonoBehaviourTest : MonoBehaviour
    {
        private Transform _transform;
    
        private void OnInit()
        {
            _transform = transform;
        }

        private void DeInit()
        {
        
        }
    }
}
