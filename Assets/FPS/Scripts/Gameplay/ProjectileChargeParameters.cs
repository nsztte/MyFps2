using Unity.FPS.Game;
using UnityEngine;

namespace Unity.FPS.Gameplay
{
    /// <summary>
    /// 충전용 발사체를 발사할때 발사체의 속성값을 설정
    /// </summary>
    public class ProjectileChargeParameters : MonoBehaviour
    {
        #region Variables
        private ProjectileBase projectileBase;

        public MinMaxFloat Damage;
        public MinMaxFloat Speed;
        public MinMaxFloat GravityDown;
        public MinMaxFloat Radius;
        #endregion

        private void OnEnable() //활성화될때
        {
            //참조
            projectileBase = GetComponent<ProjectileBase>();
            projectileBase.OnShoot += OnShoot;
        }

        //발사체 발사시 ProjectileBase의 OnShoot 델리게이트 함수에서 호출
        //발사체의 속성값을 Charge 값에 따라 설정
        void OnShoot()
        {
            //충전양에 따라 발사체의 속성값을 설정
            ProjectileStandard projectileStandard = GetComponent<ProjectileStandard>();
            projectileStandard.damage = Damage.GetValueFromRatio(projectileBase.InitialCharge);
            projectileStandard.speed = Speed.GetValueFromRatio(projectileBase.InitialCharge);
            projectileStandard.gravityDown = GravityDown.GetValueFromRatio(projectileBase.InitialCharge);
            projectileStandard.radius = Radius.GetValueFromRatio(projectileBase.InitialCharge);
        }
    }
}