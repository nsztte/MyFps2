using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.FPS.Game;
using UnityEngine.Events;

namespace Unity.FPS.Gameplay
{
    
    public enum WeaponSwitchState
    {
        Up,
        Down,               //완전히 내려간 상태
        PutDownPrevious,    //내리는 상태
        PutUpNew,           //내려온 다음 새로운 것으로 바뀔 때 올라가는 상태
    }

    /// <summary>
    /// 플레이어가 가진 무기들을 관리하는 클래스
    /// </summary>
    public class PlayerWeaponsManager : MonoBehaviour
    {
        #region Variables
        //무기 지급 - 게임 시작할때 처음 유저에게 지급되는 무기 리스트(인벤토리)
        public List<WeaponController> startingWeapons = new List<WeaponController>();


        //무기 장착
        //무기를 장착하는 오브젝트
        public Transform weaponParentSocket;

        //플레이어가 게임 중에 들고 다니는 무기 리스트
        private WeaponController[] weaponSlots = new WeaponController[9];
        
        //무기 리스트(슬롯) 중 활성화된 무기를 관리하는 인덱스
        public int ActiveWeaponIndex { get; private set; }

        //무기 교체
        public UnityAction<WeaponController> OnSwitchToWeapon;  //무기 교체시 등록된 함수 호출

        private WeaponSwitchState weaponSwitchState;        //무기 교체시 상태

        private PlayerInputHandler playerInputHandler;

        //무기 교체시 계산되는 최종 위치
        private Vector3 weaponMainLocalPosition;

        public Transform defaultWeaponPosition; //up
        public Transform downWeaponPosition;    //down

        private int weaponSwitchNewIndex;               //새로 바뀌는 무기 인덱스

        private float weaponSwitchTimeStarted = 0f;
        [SerializeField] private float weaponSwitchDelay = 1f;
        #endregion

        private void Start()
        {
            //참조
            playerInputHandler = GetComponent<PlayerInputHandler>();

            //초기화
            ActiveWeaponIndex = -1;
            weaponSwitchState = WeaponSwitchState.Down;

            //이벤트함수 등록
            OnSwitchToWeapon += OnWeaponSwitched;

            //지급받은 무기 장착
            foreach (var weapon in startingWeapons)
            {
                AddWeapon(weapon);
            }

            SwitchWeapon(true); //첫번째 무기 활성화
        }

        private void Update()
        {
            if(weaponSwitchState == WeaponSwitchState.Up || weaponSwitchState == WeaponSwitchState.Down)    //연출이 진행되는 동안엔 바뀌지 않음
            {
                int switchWeaponInput = playerInputHandler.GetSwitchWeaponInput();
                if (switchWeaponInput != 0)  //입력이 들어옴
                {
                    //입력이 들어오면(0보다 크면) 무기 변경 true
                    bool switchUp = switchWeaponInput > 0;
                    SwitchWeapon(switchUp);
                }
            }            
        }

        private void LateUpdate()
        {
            UpdateWeaponSwitching();

            //무기 최종 위치
            weaponParentSocket.localPosition = weaponMainLocalPosition;
        }

        //상태에 따른 무기 연출
        void UpdateWeaponSwitching()
        {
            //Lerp 변수
            float switchingTimeFactor = 0f;

            if(weaponSwitchDelay == 0f) //딜레이가 없으면 바로
            {
                switchingTimeFactor = 1f;
            }
            else //있으면 딜레이 이후에
            {
                switchingTimeFactor = Mathf.Clamp01(Time.time - weaponSwitchTimeStarted) / weaponSwitchDelay;
            }

            //지연시간 이후 무기 상태 변경 (딜레이 이후에 들어오는 식)
            if(switchingTimeFactor >= 1f)
            {
                if (weaponSwitchState == WeaponSwitchState.PutDownPrevious)
                {
                    //현재 무기 false, 새로운 무기 true
                    WeaponController oldWeapon = GetActiveWeapon();
                    if(oldWeapon != null)
                    {
                        oldWeapon.ShowWeapon(false);
                    }

                    ActiveWeaponIndex = weaponSwitchNewIndex;
                    WeaponController newWeapon = GetActiveWeapon();
                    OnSwitchToWeapon?.Invoke(newWeapon);

                    switchingTimeFactor = 0f;
                    if(newWeapon != null)   //바뀌는 무기가 있으면
                    {
                        weaponSwitchTimeStarted = Time.time;
                        weaponSwitchState = WeaponSwitchState.PutUpNew;
                    }
                    else //바뀌는 무기가 없으면
                    {
                        weaponSwitchState = WeaponSwitchState.Down;
                    }

                }
                else if (weaponSwitchState == WeaponSwitchState.PutUpNew)   //올라가는 상태가 지나면 Up
                {
                    weaponSwitchState = WeaponSwitchState.Up;
                }
            }

            //지연시간 동안 무기의 위치 이동
            if (weaponSwitchState == WeaponSwitchState.PutDownPrevious)
            {
                weaponMainLocalPosition = Vector3.Lerp(defaultWeaponPosition.localPosition, downWeaponPosition.localPosition, switchingTimeFactor);
            }
            else if (weaponSwitchState == WeaponSwitchState.PutUpNew)
            {
                weaponMainLocalPosition = Vector3.Lerp(downWeaponPosition.localPosition, defaultWeaponPosition.localPosition, switchingTimeFactor);
            }
        }

        //weaponSlots에 무기 프리팹으로 생성한 WeaponController 오브젝트 추가
        public bool AddWeapon(WeaponController weaponPrefab)
        {
            //추가하는 무기 소지 여부 체크 - 중복검사
            if (HasWeapon(weaponPrefab) != null)
            {
                Debug.Log("Has Same Weapon");
                return false;
            }

            for (int i = 0; i < weaponSlots.Length; i++)
            {
                //슬롯에 무기 장착
                if (weaponSlots[i] == null)
                {
                    WeaponController weaponInstance = Instantiate(weaponPrefab, weaponParentSocket);    //무기 생성
                    weaponInstance.transform.localPosition = Vector3.zero;                              //부모 위치
                    weaponInstance.transform.localRotation = Quaternion.identity;                       //기본 회전값

                    weaponInstance.Owner = this.gameObject;
                    weaponInstance.SourcePrefab = weaponPrefab.gameObject;
                    weaponInstance.ShowWeapon(false);

                    weaponSlots[i] = weaponInstance;    //슬롯에 추가

                    return true;
                }
            }
            Debug.Log("weaponSlots full");
            return false;
        }

        //매개변수로 들어온 프리팹으로 생성된 무기 여부
        private WeaponController HasWeapon(WeaponController weaponPrefab)
        {
            for (int i = 0;i < weaponSlots.Length; i++)
            {
                if (weaponSlots[i] != null && weaponSlots[i].SourcePrefab == weaponPrefab)
                {
                    return weaponSlots[i];
                }
            }
            return null;
        }

        //현재 활성화된 무기
        public WeaponController GetActiveWeapon()   
        {
            return GetWeaponAtSlotIndex(ActiveWeaponIndex);
        }

        //지정된 슬롯에 무기가 있는지 여부
        public WeaponController GetWeaponAtSlotIndex(int index)
        {
            if (index >= 0 && index < weaponSlots.Length)
            {
                return weaponSlots[index];
            }

            return null;
        }


        //0~9
        //무기 바꾸기, 현재 들고 있는 무기 false, 새로운 무기 true
        public void SwitchWeapon(bool ascendingOrder)   //ascendingOrder: 무기 순서
        {
            int newWeaponIndex = -1;    //새로 액티브할 무기 인덱스
            int closestSlotDistance = weaponSlots.Length;
            for(int i = 0; i < weaponSlots.Length; i++)
            {
                if (i != ActiveWeaponIndex && GetWeaponAtSlotIndex(i) != null)
                {
                    int distanceToActiveIndex = GetDistanceBetweenWeaponSlot(ActiveWeaponIndex, i, ascendingOrder);
                    if (distanceToActiveIndex < closestSlotDistance)
                    {
                        closestSlotDistance = distanceToActiveIndex;
                        newWeaponIndex = i;
                    }
                }
            }

            //새로 액티브할 무기 인덱스로 무기 교체
            SwitchToWeaponIndex(newWeaponIndex);
        }

        private void SwitchToWeaponIndex(int newWeaponIndex)
        {
            //newWeaponIndex 값 체크
            if (newWeaponIndex >= 0 && newWeaponIndex != ActiveWeaponIndex)
            {
                weaponSwitchNewIndex = newWeaponIndex;
                weaponSwitchTimeStarted = Time.time;

                //현재 액티브한 무기가 있는지?
                if(GetActiveWeapon() == null)   //Down인 상태
                {
                    weaponMainLocalPosition = downWeaponPosition.position;
                    weaponSwitchState = WeaponSwitchState.PutUpNew;
                    ActiveWeaponIndex = newWeaponIndex;

                    WeaponController weaponController = GetWeaponAtSlotIndex(newWeaponIndex);
                    OnSwitchToWeapon?.Invoke(weaponController);
                }
                else   //Up인 상태
                {
                    weaponSwitchState = WeaponSwitchState.PutDownPrevious;
                }



                //if(ActiveWeaponIndex >= 0)
                //{
                //    WeaponController nowWeapon = GetWeaponAtSlotIndex(ActiveWeaponIndex);
                //    nowWeapon.ShowWeapon(false);
                //}
                //WeaponController newWeapon = GetWeaponAtSlotIndex(newWeaponIndex);
                //newWeapon.ShowWeapon(true);
                //ActiveWeaponIndex = newWeaponIndex;
            }
        }

        //슬롯간 거리
        private int GetDistanceBetweenWeaponSlot(int fromSlotIndex, int toSlotIndex, bool ascendingOrder)
        {
            int distanceBetweenSlots = 0;
            
            if(ascendingOrder)  //오름차순
            {
                distanceBetweenSlots = toSlotIndex - fromSlotIndex;
            }
            else
            {
                distanceBetweenSlots = distanceBetweenSlots - toSlotIndex;
            }

            if(distanceBetweenSlots < 0)
            {
                distanceBetweenSlots = distanceBetweenSlots + weaponSlots.Length;
            }

            return distanceBetweenSlots;
        }

        void OnWeaponSwitched(WeaponController newWeapon)
        {
            if(newWeapon != null)
            {
                newWeapon.ShowWeapon(true);
            }
        }
    }
}