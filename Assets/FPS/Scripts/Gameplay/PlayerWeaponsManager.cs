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
    /// 플레이어가 가진 무기(WeaponController)들을 관리하는 클래스
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
        public UnityAction<WeaponController> OnSwitchToWeapon;      //무기 교체할때마다 등록된 함수 호출
        public UnityAction<WeaponController, int> OnAddedWeapon;    //무기 추가할때마다 등록된 함수 호출
        public UnityAction<WeaponController, int> OnRemoveWeapon;   //장착된 무기를 제거할때마다 등록된 함수 호출

        private WeaponSwitchState weaponSwitchState;        //무기 교체시 상태

        private PlayerInputHandler playerInputHandler;

        //무기 교체시 계산되는 최종 위치
        private Vector3 weaponMainLocalPosition;

        public Transform defaultWeaponPosition; //up
        public Transform downWeaponPosition;    //down
        public Transform aimingWeaponPosition;

        private int weaponSwitchNewIndex;               //새로 바뀌는 무기 인덱스

        private float weaponSwitchTimeStarted = 0f;
        [SerializeField] private float weaponSwitchDelay = 1f;

        //적 포착
        public bool IsPointingAtEnemy { get; private set; }         //적 포착 여부
        public Camera weaponCamera;                                 //weaponCamera에서 Ray로 적 확인

        //조준
        //카메라 셋팅
        private PlayerCharacterController playerCharacterController;
        [SerializeField] private float defaultFov = 60f;              //카메라 기본 FOV 값(줌인줌아웃)
        [SerializeField] private float weaponFovMultiplier = 1f;      //무기별 FOV 연산 계수

        public bool IsAiming { get; private set; }                      //무기 조준 여부
        [SerializeField] private float aimingAnimationSpeed = 10f;      //무기 이동, Fov 연출 Lerp 속도

        //흔들림
        [SerializeField] private float bobFrequency = 10f;
        [SerializeField] private float bobSharpness = 10f;
        [SerializeField] private float defaultBobAmount = 0.05f;     //평상시 흔들림 양
        [SerializeField] private float aimingBobAmount = 0.02f;      //조준 중 흔들림 양

        private float weaponBobFactor;              //흔들림 계수
        private Vector3 lastCharacterPosition;      //현재 프레임에서의 이동속도를 구하기 위한 변수

        private Vector3 weaponBobLocalPosition;     //이동시 흔들림 량 최종 계산값, 이동하지 않으면 0

        //반동
        [SerializeField] private float recoilSharpness = 50f;       //이동 속도
        [SerializeField] private float maxRecoilDistance = 0.5f;    //반동시 뒤로 밀릴 수 있는 최대 거리
        private float recoilRepositionSharpness = 10f;              //제자리로 돌아오는 속도
        private Vector3 accumulateRecoil;                           //반동시 뒤로 밀리는 양

        private Vector3 weaponRecoilLocalPosition;      //반동시 이동한 최종 계산값, 반동 후 제자리에 돌아오면 0

        //저격 모드
        private bool isScopeOn = false;
        [SerializeField] private float distanceOnScope = 0.1f;

        public UnityAction OnScopedWeapon;              //저격 모드 시작시 등록된 함수 호출
        public UnityAction OffScopedWeapon;             //저격 모드 끝낼때 등록된 함수 호출
        #endregion

        private void Start()
        {
            //참조
            playerInputHandler = GetComponent<PlayerInputHandler>();
            playerCharacterController = GetComponent<PlayerCharacterController>();

            //초기화
            ActiveWeaponIndex = -1;
            weaponSwitchState = WeaponSwitchState.Down;

            //액티브무기 show 함수 등록
            OnSwitchToWeapon += OnWeaponSwitched;

            //저격모드 함수 등록
            OnScopedWeapon += OnScope;
            OffScopedWeapon += OffScope;

            //Fov 초기값 설정
            SetFov(defaultFov);

            //지급받은 무기 장착
            foreach (var weapon in startingWeapons)
            {
                AddWeapon(weapon);
            }

            SwitchWeapon(true); //첫번째 무기 활성화
        }

        private void Update()
        {
            //현재 액티브 무기
            WeaponController activeWeapon = GetActiveWeapon();

            //무기가 손 위에 올라왔을 때 조준
            if (weaponSwitchState == WeaponSwitchState.Up)
            {
                //조준 입력값 처리
                IsAiming = playerInputHandler.GetAimInputHeld();

                //저격 모드 처리
                if(activeWeapon.shootType == WeaponShootType.Sniper)
                {
                    if(playerInputHandler.GetAimInputDown())
                    {
                        //저격 모드 시작
                        isScopeOn = true;
                        //OnScopedWeapon?.Invoke();
                    }
                    if(playerInputHandler.GetAimInputUp())
                    {
                        //저격 모드 끝
                        OffScopedWeapon?.Invoke();
                    }
                }

                //슛 처리
                bool isFire = activeWeapon.HandleShootInputs(
                    playerInputHandler.GetFireInputDown(),
                    playerInputHandler.GetFireInputHeld(),
                    playerInputHandler.GetFireInputUp());

                if(isFire)
                {
                    //반동 효과
                    accumulateRecoil += Vector3.back * activeWeapon.recoilForce;
                    accumulateRecoil = Vector3.ClampMagnitude(accumulateRecoil, maxRecoilDistance);
                }
            }

            if (!IsAiming && (weaponSwitchState == WeaponSwitchState.Up || weaponSwitchState == WeaponSwitchState.Down))    //조준하면서 연출이 진행되는 동안엔 바뀌지 않음
            {
                int switchWeaponInput = playerInputHandler.GetSwitchWeaponInput();
                if (switchWeaponInput != 0)  //입력이 들어옴
                {
                    //입력이 들어오면(0보다 크면) 무기 변경 true
                    bool switchUp = switchWeaponInput > 0;
                    SwitchWeapon(switchUp);
                }
            }

            //적 포착
            IsPointingAtEnemy = false;  //기본 상태
            if (activeWeapon)
            {
                RaycastHit hit;
                if(Physics.Raycast(weaponCamera.transform.position, weaponCamera.transform.forward, out hit, 300))
                {
                    //콜라이더 체크 - 적(Health) 판별
                    Health health = hit.collider.GetComponent<Health>();
                    if(health)
                    {
                        IsPointingAtEnemy = true;
                    }
                }
            }
        }

        private void LateUpdate()
        {
            UpdateWeaponBob();
            UpdateWeaponRecoil();
            UpdateWeaponAming();
            UpdateWeaponSwitching();

            //무기 최종 위치
            weaponParentSocket.localPosition = weaponMainLocalPosition + weaponBobLocalPosition + weaponRecoilLocalPosition;    //+ 흔들림, 반동 조정값(이동하지 않으면 0)
        }

        //반동
        void UpdateWeaponRecoil()
        {
            if(weaponRecoilLocalPosition.z >= accumulateRecoil.z * 0.99f) //이동한 위치가 뒤로 밀리는 양보다 크면
            {
                //accumulateRecoil 위치로 조정
                weaponRecoilLocalPosition = Vector3.Lerp(weaponRecoilLocalPosition, accumulateRecoil, recoilSharpness * Time.deltaTime);
            }
            else
            {
                //반동이 끝나면 원위치로
                weaponRecoilLocalPosition = Vector3.Lerp(weaponRecoilLocalPosition, Vector3.zero, recoilRepositionSharpness * Time.deltaTime);
                accumulateRecoil = weaponRecoilLocalPosition;   //반동시 밀리는 양 0으로 초기화
            }
        }


        //카메라 Fov 값 셋팅: 줌인, 줌아웃
        private void SetFov(float fov)
        {
            playerCharacterController.PlayerCamera.fieldOfView = fov;
            weaponCamera.fieldOfView = fov * weaponFovMultiplier;
        }

        //무기 조준에 따른 연출, 무기 위치 조정, Fov값 조정
        void UpdateWeaponAming()
        {
            //무기를 들고 있을때만 조준 가능
            if (weaponSwitchState == WeaponSwitchState.Up)
            {
                WeaponController activeWeapon = GetActiveWeapon();

                if (IsAiming && activeWeapon)   //조준시: 디폴트 포지션 -> Aiming 포지션 위치로 이동, Fov: 디폴트 -> aimZoomRatio
                {
                    //포지션
                    weaponMainLocalPosition = Vector3.Lerp(weaponMainLocalPosition,
                        aimingWeaponPosition.localPosition + activeWeapon.aimOffset,
                        aimingAnimationSpeed * Time.deltaTime);

                    //저격 모드 시작
                    if(isScopeOn)
                    {
                        //weaponMainLocalPosition, 목표지점까지의 거리를 구한다
                        float dist = Vector3.Distance(weaponMainLocalPosition, aimingWeaponPosition.localPosition);
                        if (dist < distanceOnScope)
                        {
                            OnScopedWeapon?.Invoke();
                            isScopeOn = false;
                        }
                    }
                    else //거리가 먼저 가까워진 다음에 줌
                    {
                        //Fov
                        float Fov = Mathf.Lerp(playerCharacterController.PlayerCamera.fieldOfView,
                            activeWeapon.aimZoomRatio * defaultFov, aimingAnimationSpeed * Time.deltaTime);
                        SetFov(Fov);
                    }

                    
                }
                else            //조준이 풀렸을때: Aiming 포지션 -> 디폴트 포지션 위치로 이동, Fov: aimZoomRatio -> 디폴트
                {
                    //포지션
                    weaponMainLocalPosition = Vector3.Lerp(weaponMainLocalPosition,
                        defaultWeaponPosition.localPosition,
                        aimingAnimationSpeed * Time.deltaTime);
                    //Fov
                    float Fov = Mathf.Lerp(playerCharacterController.PlayerCamera.fieldOfView,
                        defaultFov, aimingAnimationSpeed * Time.deltaTime);
                    SetFov(Fov);
                }
            }
        }

        //이동에 의한 무기 흔들림 값 구하기
        void UpdateWeaponBob()
        {
            if(Time.deltaTime > 0)  //한 프레임 돌았을때
            {
                //플레이어가 한 프레임동안 이동한 거리
                //playerCharacterController.transform.position - lastCharacterPosition

                //현재 프레임에서 플레이어 이동 속도
                Vector3 playerCharacterVelocity =
                    (playerCharacterController.transform.position - lastCharacterPosition) / Time.deltaTime;

                float characterMovementFactor = 0f;
                if(playerCharacterController.IsGrounded)    //땅에서 움직일 때만 흔들림
                {
                    characterMovementFactor = Mathf.Clamp01(playerCharacterVelocity.magnitude /
                    (playerCharacterController.MaxSpeedOnGround * playerCharacterController.SprintSpeedModifier));
                }

                //속도에 의한 흔들림 계수
                weaponBobFactor = Mathf.Lerp(weaponBobFactor, characterMovementFactor, bobSharpness * Time.deltaTime);

                //흔들림량(조준시, 평상시)
                float bobAmount = IsAiming ? aimingBobAmount : defaultBobAmount;
                float frequency = bobFrequency;

                //좌우 흔들림
                float hBobValue = Mathf.Sin(Time.time * frequency) * bobAmount * weaponBobFactor;
                //위아래 흔들림
                float VBobValue = ((Mathf.Sin(Time.time * frequency) * 0.5f) + 0.5f) * bobAmount * weaponBobFactor;    //사인 곡선 1~0

                //흔들림 최종 변수에 적용
                weaponBobLocalPosition.x = hBobValue;
                weaponBobLocalPosition.y = Mathf.Abs(VBobValue);

                //플레이어의 현재 프레임의 마지막 위치를 저장
                lastCharacterPosition = playerCharacterController.transform.position;
            }
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
                switchingTimeFactor = Mathf.Clamp01(Time.time - weaponSwitchTimeStarted) / weaponSwitchDelay;   //0~1
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

                    //무기장착
                    OnAddedWeapon?.Invoke(weaponInstance, i);

                    weaponSlots[i] = weaponInstance;    //슬롯에 추가
                    return true;
                }
            }
            Debug.Log("weaponSlots full");
            return false;
        }

        //weaponSlots에 장착된 무기 제거
        public bool RemoveWeapon(WeaponController oldWeapon)
        {
            for(int i = 0; i < weaponSlots.Length; i++)
            {
                //같은 무기 찾아서 제거
                if(weaponSlots[i] == oldWeapon)
                {
                    //제거
                    weaponSlots[i] = null;

                    OnRemoveWeapon?.Invoke(oldWeapon, i);

                    Destroy(oldWeapon.gameObject);

                    //현재 제거한 무기가 액티브이면 새로운 액티브 무기를 찾는다
                    if(i == ActiveWeaponIndex)
                    {
                        SwitchWeapon(true);
                    }

                    return true;
                }
            }

            return false;
        }


        //매개변수로 들어온 프리팹으로 생성된 무기 여부
        private WeaponController HasWeapon(WeaponController weaponPrefab)
        {
            for (int i = 0; i < weaponSlots.Length; i++)
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
                distanceBetweenSlots = fromSlotIndex - toSlotIndex;
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

        void OnScope()
        {
            weaponCamera.enabled = false;
        }

        void OffScope()
        {
            weaponCamera.enabled = true;
        }
    }
}