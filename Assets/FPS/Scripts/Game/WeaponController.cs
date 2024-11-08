using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.FPS.Game
{
    /// <summary>
    /// 크로스헤어를 그리기위한 데이터
    /// </summary>
    [System.Serializable]
    public struct CrossHairData
    {
        public Sprite CrossHairSprite;
        public float CrossHairSize;
        public Color CrossHairColor;
    }

    /// <summary>
    /// 무기 슛 타입
    /// </summary>
    public enum WeaponShootType
    {
        Manual,
        Automatic,
        Charge,
        Sniper
    }

    /// <summary>
    /// 무기(총기)를 관리하는 클래스
    /// </summary>
    
    public class WeaponController : MonoBehaviour
    {
        #region Variables
        //무기 활성화, 비활성화
        public GameObject weaponRoot;
        public GameObject Owner { get; set; }               //무기 주인
        public GameObject SourcePrefab { get; set; }        //무기를 생성한 오리지널 프리팹
        public bool IsWeaponActive { get; private set; }    //무기 활성화 여부

        private AudioSource shootAudioSource;
        public AudioClip switchWeaponSfx;

        //shooting
        public WeaponShootType shootType;

        [SerializeField] private float maxAmmo = 8f;                //장전할 수 있는 최대 총알 갯수
        private float currentAmmo;

        [SerializeField] private float delayBetweenShots = 0.5f;    //슛 간격
        private float lastTimeShot;                                 //마지막으로 슛한 시간

        //Vfx, Sfx
        public Transform weaponMuzzle;                              //총구 위치
        public GameObject muzzleFlashPrefab;                        //총구 발사 효과
        public AudioClip shootSfx;                                  //총 발사 사운드


        //CrossHair
        public CrossHairData crossHairDefault;          //기본, 평상시
        public CrossHairData crossHairTargetInSight;    //적을 포착했을때, 타겟팅 되었을때

        //조준
        public float aimZoomRatio = 1f;     //조준시 줌인 설정값
        public Vector3 aimOffset;           //조준시 무기 위치 조정값

        //반동
        public float recoilForce = 0.5f;

        //projectile
        public ProjectileBase projectilePrefab;

        //projectile
        public Vector3 MuzzleWorldVelocity { get; private set; }                    //현재 속도
        private Vector3 lastMuzzlePosition;
        public float CurrentCharge { get; private set; }

        [SerializeField] private int bulletPerShot = 1;                              //한번 슛하는데 발사되는 탄환의 갯수
        [SerializeField] private float bulletSpreadAngle = 0f;                       //뷸렛이 퍼져나가는 각도
        #endregion

        public float CurrentAmmoRatio => currentAmmo / maxAmmo;

        public void Awake()
        {
            //참조
            shootAudioSource = GetComponent<AudioSource>();
        }

        private void Start()
        {
            //초기화
            currentAmmo = maxAmmo;
            lastTimeShot = Time.time;
            lastMuzzlePosition = weaponMuzzle.position;
        }

        private void Update()
        {
            //MuzzleWorldVelocity
            if(Time.deltaTime > 0f)  //한 프레임이 돌았다
            {
                //한 프레임에서 이동 속도
                MuzzleWorldVelocity = (weaponMuzzle.position - lastMuzzlePosition) / Time.deltaTime;    //속도 = 거리 / 시간

                lastMuzzlePosition = weaponMuzzle.position;
            }
        }


        //무기 활성화, 비활성화
        public void ShowWeapon(bool show)
        {
            weaponRoot.SetActive(show);

            //this 무기로 변경
            if(show && switchWeaponSfx != null)
            {
                //무기 변경 효과음 플레이
                shootAudioSource.PlayOneShot(switchWeaponSfx);
            }
            IsWeaponActive = show;
        }

        //키 입력에 따른 슛 타입 구현
        public bool HandleShootInputs(bool inputDown, bool inputHeld, bool inputUp)
        {
            switch(shootType)
            {
                case WeaponShootType.Manual:
                    if(inputDown)
                    {
                        return TryShoot();
                    }
                    break;
                case WeaponShootType.Automatic:
                    if(inputHeld)
                    {
                        return TryShoot();
                    }
                    break;
                case WeaponShootType.Charge:
                    break;
                case WeaponShootType.Sniper:
                    if (inputDown)
                    {
                        return TryShoot();
                    }
                    break;
            }

            return false;
        }


        bool TryShoot()
        {
            //
            if(currentAmmo >= 1f && (lastTimeShot + delayBetweenShots) < Time.time)    //총알이 한 발 이상이고 현재 프레임 시간이 딜레이가 지난 시간이면 슛
            {
                currentAmmo -= 1f;
                Debug.Log($"currentAmmo: {currentAmmo}");

                HandleShoot();

                return true;
            }
            return false;
        }

        //슛 연출
        void HandleShoot()
        {
            //projectile 생성
            for(int i = 0; i < bulletPerShot; i++)
            {
                Vector3 shootDirection = GetShotDirectionWithinSpread(weaponMuzzle);
                ProjectileBase projectileInstance = Instantiate(projectilePrefab, weaponMuzzle.position, Quaternion.LookRotation(shootDirection));
                projectileInstance.Shoot(this);
            }
            

            //Vfx
            if(muzzleFlashPrefab)   //Launcher은 없음
            {
                GameObject effectGo = Instantiate(muzzleFlashPrefab, weaponMuzzle.position, weaponMuzzle.rotation, weaponMuzzle);
                Destroy(effectGo, 1f);
            }

            //Sfx
            if(shootSfx)
            {
                shootAudioSource.PlayOneShot(shootSfx);
            }

            //마지막으로 슛한 시간 저장
            lastTimeShot = Time.time;
        }

        //projectile 날아가는 방향
        Vector3 GetShotDirectionWithinSpread(Transform shootTransform)
        {
            float spreadAngleRatio = bulletSpreadAngle / 180f;
            return Vector3.Lerp(shootTransform.forward, UnityEngine.Random.insideUnitSphere, spreadAngleRatio);
        }
    }
}