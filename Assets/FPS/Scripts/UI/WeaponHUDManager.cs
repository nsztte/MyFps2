using UnityEngine;
using Unity.FPS.Game;
using Unity.FPS.Gameplay;

namespace Unity.FPS.UI
{
    public class WeaponHUDManager : MonoBehaviour
    {
        #region Variables
        public RectTransform ammoPanel;             //ammoCountUI 부모 오브젝트
        public GameObject ammoCountPrefab;          //ammoCountUI 프리팹

        private PlayerWeaponsManager playerWeaponsManager;
        #endregion

        private void Awake()
        {
            //참조
            playerWeaponsManager = GameObject.FindObjectOfType<PlayerWeaponsManager>();

            //이벤트함수 등록
            playerWeaponsManager.OnAddedWeapon += AddWeapon;
            playerWeaponsManager.OnRemoveWeapon += RemoveWeapon;
            playerWeaponsManager.OnSwitchToWeapon += SwitchWeapon;
        }

        //무기 추가하면 ammo UI 하나 추가
        void AddWeapon(WeaponController newWeapon, int weaponIndex)
        {
            GameObject ammoCountGo = Instantiate(ammoCountPrefab, ammoPanel);
            AmmoCount ammoCount = ammoCountGo.GetComponent<AmmoCount>();
            ammoCount.Initialize(newWeapon, weaponIndex);
        }

        //무기 제거하면 ammo UI 하나 제거
        void RemoveWeapon(WeaponController newWeapon, int weaponIndex)
        {
            //월요일에 함
        }

        //무기 바꿀때 UI 재정렬
        void SwitchWeapon(WeaponController weapon)  //매개변수 이용하지 않아도 이벤트 함수 등록하기 위해서 써야함
        {
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(ammoPanel);
        }
    }
}