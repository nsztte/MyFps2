using UnityEngine;
using UnityEngine.UI;
using Unity.FPS.Gameplay;
using Unity.FPS.Game;
using TMPro;

namespace Unity.FPS.UI
{
    /// <summary>
    /// WeaponController 무기의 Ammo 카운트 UI 
    /// </summary>
    public class AmmoCount : MonoBehaviour
    {
        #region Variables
        private PlayerWeaponsManager playerWeaponsManager;

        private WeaponController weaponController;
        private int weaponIndex;

        //UI
        public TextMeshProUGUI weaponIndexText;

        public Image ammoFillImage;                                 //ammo rate에 따른 게이지

        [SerializeField] private float ammoFillSharpness = 10f;     //게이지 채우는(비우는) 속도
        private float weaponSwitchSharpness = 10f;                  //무기 교체시 UI가 바뀌는 속도

        public CanvasGroup canvasGroup;
        [SerializeField] [Range(0, 1)] private float unSelectedOpacity = 0.5f;
        private Vector3 unSelectedScale = Vector3.one * 0.8f;

        //게이지바 색 변경
        public FillBarColorChange fillBarColorChange;
        #endregion

        //AmmoCount UI 값 초기화
        public void Initialize(WeaponController weapon, int _weaponIndex)
        {
            weaponController = weapon;
            weaponIndex = _weaponIndex;

            //무기 인덱스
            weaponIndexText.text = (weaponIndex + 1).ToString();    //weaponIndex는 0부터 시작하니까 1부터 시작하게 수정

            //게이지색 초기화
            fillBarColorChange.Initialize(1f, 0.1f);

            //참조
            playerWeaponsManager = GameObject.FindObjectOfType<PlayerWeaponsManager>();
        }

        private void Update()
        {
            //총알 비율에 따라 이미지 채우기
            float currentFillRate = weaponController.CurrentAmmoRatio;
            ammoFillImage.fillAmount = Mathf.Lerp(ammoFillImage.fillAmount, currentFillRate, ammoFillSharpness * Time.deltaTime);

            //액티브 무기 구분
            bool isActiveWeapon = (weaponController == playerWeaponsManager.GetActiveWeapon());
            float currentOpacity = isActiveWeapon ? 1.0f : unSelectedOpacity;   //투명도
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, currentOpacity, weaponSwitchSharpness * Time.deltaTime);
            Vector3 currentScale = isActiveWeapon ? Vector3.one : unSelectedScale;  //스케일
            transform.localScale = Vector3.Lerp(transform.localScale, currentScale, weaponSwitchSharpness * Time.deltaTime);

            //배경색 변경
            fillBarColorChange.UpdateVisual(currentFillRate);
        }
    }
}