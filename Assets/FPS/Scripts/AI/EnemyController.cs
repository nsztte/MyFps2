using UnityEngine;
using Unity.FPS.Game;
using UnityEngine.Events;
using System.Collections.Generic;
using UnityEngine.AI;

namespace Unity.FPS.AI
{
    /// <summary>
    /// 렌더러 데이터: 메테리얼 정보 저장
    /// </summary>
    [System.Serializable]
    public struct RendererIndexData
    {
        public Renderer renderer;
        public int materialIndex;

        public RendererIndexData(Renderer _renderer, int index)
        {
            renderer = _renderer;
            materialIndex = index;
        }
    }

    /// <summary>
    /// ememy를 관리하는 클래스
    /// </summary>
    public class EnemyController : MonoBehaviour
    {
        #region Variables
        private Health health;

        //죽음
        public GameObject deathVfxPrefab;
        public Transform deathVfxSpawnPosition;

        //damage
        public UnityAction Damaged;

        //Sfx
        public AudioClip damageSfx;

        //Vfx
        public Material bodyMaterial;               //데미지를 줄 메테리얼
        [GradientUsage(true)]
        public Gradient onHitBodyGradient;          //데미지를 컬러 그라디언트 효과로 표현
        //body material을 가지고 있는 렌더러 데이터 리스트
        private List<RendererIndexData> bodyRenderers = new List<RendererIndexData>();
        MaterialPropertyBlock bodyFlashMaterialPropertyBlock;

        [SerializeField] private float flashOwnHitDuratoin = 0.5f;
        float lastTimeDamaged = float.NegativeInfinity;
        bool wasDamagedThisFrame = false;

        //Patrol
        public NavMeshAgent Agent { get; private set; }
        public PatrolPath PatrolPath { get; set; }
        private int pathDestinationIndex;               //목표 웨이포인트 인덱스
        private float pathReachingRadius = 1f;          //도착판정

        #endregion

        private void Start()
        {
            //참조
            Agent = GetComponent<NavMeshAgent>();
            health = GetComponent<Health>();

            health.OnDamaged += OnDamaged;
            health.OnDie += OnDie;

            //bodyMaterial을 가지고 있는 렌더러 정보 리스트 만들기
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                for (int i = 0; i < renderer.sharedMaterials.Length; i++)   //sharedMaterials은 배열의 형태
                {
                    if (renderer.sharedMaterials[i] == bodyMaterial)
                    {
                        bodyRenderers.Add(new RendererIndexData(renderer, i));
                    }
                }
            }

            //
            bodyFlashMaterialPropertyBlock = new MaterialPropertyBlock();
        }

        private void Update()
        {
            //데미지 효과
            Color currentColor = onHitBodyGradient.Evaluate((Time.time - lastTimeDamaged) / flashOwnHitDuratoin);
            bodyFlashMaterialPropertyBlock.SetColor("_EmissionColor", currentColor);
            foreach(var data in bodyRenderers)
            {
                data.renderer.SetPropertyBlock(bodyFlashMaterialPropertyBlock, data.materialIndex);
            }

            //
            wasDamagedThisFrame = false;
        }

        private void OnDamaged(float damage, GameObject damageSource)
        {
            if(damageSource && damageSource.GetComponent<EnemyController>() == null)   //팀킬이나 자기자신 공격 방지
            {
                //등록된 함수 호출
                Damaged?.Invoke();

                //데미지 받은 시간
                lastTimeDamaged = Time.time;

                //Sfx
                if (damageSfx && wasDamagedThisFrame == false)
                {
                    AudioUtility.CreateSfx(damageSfx, this.transform.position, 0f);
                }
                wasDamagedThisFrame = true;
            }
        }

        private void OnDie()
        {
            //폭발 효과
            GameObject vfxGo = Instantiate(deathVfxPrefab, deathVfxSpawnPosition.position, Quaternion.identity);
            Destroy(vfxGo, 5f);

            //Enemy 킬
            Destroy(gameObject);
        }

        //패트롤이 유효한 길인지 판단
        private bool IsPathValid()
        {
            return PatrolPath && PatrolPath.wayPoints.Count > 0;
        }

        //가장 가까운 WayPoint 찾기
        private void SetPathDestinationToCloseWayPoint()
        {
            if (IsPathValid() == false)
            {
                pathDestinationIndex = 0;
                return;
            }

            int closestWayPointIndex = 0;
            for (int i = 0; i< PatrolPath.wayPoints.Count; i++)
            {
                float distance = PatrolPath.GetDistanceToWayPoint(transform.position, i);
                float ClosestDistance = PatrolPath.GetDistanceToWayPoint(transform.position, closestWayPointIndex);
                if(distance < ClosestDistance)
                {
                    closestWayPointIndex = i;
                }
            }
            pathDestinationIndex = closestWayPointIndex;
        }

        //목표 지점의 위치 값 얻어오기
        public Vector3 GetDestinationPath()
        {
            if (IsPathValid() == false)
            {
                return this.transform.position; //현재 목표 지점에 있음을 반환
            }

            return PatrolPath.GetPositionOfWayPoint(pathDestinationIndex);
        }

        //목표 지점 설정 - Nav 시스템 이용
        public void SetNavDestination(Vector3 destination)
        {
            if(Agent)
            {
                Agent.SetDestination(destination);
            }
        }

        //도착 판정 후 다음 목표지점 설정
        public void UpdatePathDestination(bool inverseOrder = false)    //inverseOrder: 내림차순
        {
            if (IsPathValid() == false)
                return;
            
            //도착판정
            float distance = (transform.position - GetDestinationPath()).magnitude;
            if(distance <= pathReachingRadius)
            {
                pathDestinationIndex = inverseOrder ? (pathDestinationIndex - 1) : (pathDestinationIndex + 1);
                if(pathDestinationIndex < 0)
                {
                    pathDestinationIndex += PatrolPath.wayPoints.Count;
                }
                if(pathDestinationIndex >= PatrolPath.wayPoints.Count)
                {
                    pathDestinationIndex -= PatrolPath.wayPoints.Count;
                }
            }
        }
    }
}