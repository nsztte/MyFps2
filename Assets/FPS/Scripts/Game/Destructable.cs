using UnityEngine;

namespace Unity.FPS.Game
{
    /// <summary>
    /// 죽었을 때 Health를 가진 오브젝트를 킬하는 클래스
    /// </summary>
    public class Destructable : MonoBehaviour
    {
        #region Variables
        private Health health;
        #endregion

        private void Start()
        {
            //참조
            health = GetComponent<Health>();

            //컴포넌트가 null이면 디버그하는 함수 호출
            DebugUtility.HandleErrorIfNullGetComponent<Health,Destructable>(health, this, gameObject);

            //UnityAction 함수에 등록
            health.OnDamaged += OnDamaged;
            health.OnDie += OnDie;
        }

        void OnDamaged(float damage, GameObject damageSource)
        {

        }

        void OnDie()
        {
            //킬
            Destroy(gameObject);
        }
    }
}