using Unity.FPS.Game;
using UnityEngine;

namespace Unity.FPS.Gameplay
{
    /// <summary>
    /// 충전용 발사체를 발사할때 충전량에 발사체의 게임오브젝트 크기 설정
    /// </summary>
    public class ChargeProjectileEffectHandler : MonoBehaviour
    {
        #region Variables
        private ProjectileBase projectileBase;

        public GameObject chargeObject;
        public MinMaxVector3 scale;
        #endregion

        private void OnEnable() //활성화될때
        {
            //참조
            projectileBase = GetComponent<ProjectileBase>();
            projectileBase.OnShoot += OnShoot;
        }

        void OnShoot()
        {
            chargeObject.transform.localScale = scale.GetValueFromRatio(projectileBase.InitialCharge);
        }
    }
}