using UnityEngine;
using UnityEngine.UI;
using Unity.FPS.Game;
using Unity.FPS.Gameplay;

namespace Unity.FPS.UI
{
    public class CrossHairManager : MonoBehaviour
    {
        #region Variables
        public Image crosshairImage;                    //크로스헤어 UI 이미지
        public Sprite nullCrosshairSprite;              //액티브한 무기가 없을때

        private RectTransform crosshairRectTransform;
        private CrossHairData crosshairDefault;         //평상시, 기본
        private CrossHairData crosshairTarget;          //타겟팅 되었을때

        private CrossHairData crosshairCurrent;         //실질적으로 그리는 크로스헤어
        [SerializeField] private float crosshairUpdateSharpness = 5.0f;  //Lerp 변수(속도)

        private PlayerWeaponsManager weaponManager;

        private bool wasPointingAtEnemy;                //타겟팅 순간 포착하기 위한 변수
        #endregion

        private void Start()
        {
            //참조
            weaponManager = GameObject.FindObjectOfType<PlayerWeaponsManager>();

            //액티브한 무기 크로스 헤어 보이기 (weaponManager start 함수 순서 꼬임 방지)
            OnWeaponChanged(weaponManager.GetActiveWeapon());

            weaponManager.OnSwitchToWeapon += OnWeaponChanged;
        }

        private void Update()
        {
            UpdateCrosshairPointingAtEnemy(false);

            wasPointingAtEnemy = weaponManager.IsPointingAtEnemy;   //타겟팅 순간 저장
        }

        //크로스 헤어 그리기
        void UpdateCrosshairPointingAtEnemy(bool force)
        {
            if (crosshairDefault.CrossHairSprite == null)
                return;

            //평상시?, 타겟팅?
            //상태가 변하는 경계 지점 포착
            //force로 true일때 강제로 한번 그리고 시작
            if((force || wasPointingAtEnemy == false) && weaponManager.IsPointingAtEnemy == true)      //적을 포착하는 순간
            {
                crosshairCurrent = crosshairTarget;
                crosshairImage.sprite = crosshairCurrent.CrossHairSprite;
                crosshairRectTransform.sizeDelta = crosshairCurrent.CrossHairSize * Vector2.one;
            }
            else if((force || wasPointingAtEnemy == true) && weaponManager.IsPointingAtEnemy == false) //적을 놓치는 순간
            {
                crosshairCurrent = crosshairDefault;
                crosshairImage.sprite = crosshairCurrent.CrossHairSprite;
                crosshairRectTransform.sizeDelta = crosshairCurrent.CrossHairSize * Vector2.one;
            }

            crosshairImage.color = Color.Lerp(crosshairImage.color,
                crosshairCurrent.CrossHairColor, crosshairUpdateSharpness * Time.deltaTime);
            crosshairRectTransform.sizeDelta = Mathf.Lerp(crosshairRectTransform.sizeDelta.x,
                crosshairCurrent.CrossHairSize, crosshairUpdateSharpness * Time.deltaTime) * Vector2.one;
        }

        //무기가 바뀔때마다 crosshairImage를 각각의 무기 crosshair 이미지로 바꾸기
        void OnWeaponChanged(WeaponController newWeapon)
        {
            if(newWeapon)   //!=null : true
            {
                crosshairImage.enabled = true;
                crosshairRectTransform = crosshairImage.GetComponent<RectTransform>();
                //액티브 무기의 크로스헤어 정보 가져오기
                crosshairDefault = newWeapon.crossHairDefault;
                crosshairTarget = newWeapon.crossHairTargetInSight;
            }
            else
            {
                if(nullCrosshairSprite)
                {
                    crosshairImage.sprite = nullCrosshairSprite;
                }
                else
                {
                    crosshairImage.enabled = false;
                }
            }

            UpdateCrosshairPointingAtEnemy(true);   //처음 시작할때 강제로 한번 그리기
        }



    }
}