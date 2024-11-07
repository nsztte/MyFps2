using UnityEngine;
using UnityEngine.Events;

namespace Unity.FPS.Game
{
    /// <summary>
    /// 발사체의 기본이 되는 부모 클래스
    /// </summary>
    public abstract class ProjectileBase : MonoBehaviour
    {
        #region Variables
        public GameObject Owner { get; private set; }   //발사한 주체
        public Vector3 InitialPosition { get; private set; }
        public Vector3 InitialDirection { get; private set; }
        public Vector3 InheritedMuzzleVelocity { get; private set; }    //총구 속도
        public float InitialCharge { get; private set; }

        public UnityAction OnShoot;                     //발사시 등록된 함수 호출
        #endregion

        public void Shoot(WeaponController controller)
        {
            Owner = controller.Owner;
            InitialPosition = this.transform.position;  //처음 쐈을때는 프로젝타일이 총구의 위치
            InitialDirection = this.transform.forward;  //처음엔 앞방향
            InheritedMuzzleVelocity = controller.MuzzleWorldVelocity;
            InitialCharge = controller.CurrentCharge;

            OnShoot?.Invoke();
        }
    }
}