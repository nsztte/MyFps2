using Unity.FPS.Game;
using UnityEngine;

namespace Unity.FPS.AI
{
    /// <summary>
    /// Enemy 상태
    /// </summary>
    public enum AIState
    {
        Patrol,
        Follow,
        Attack
    }

    /// <summary>
    /// 이동하는 Enemy의 상태들을 구현하는 클래스
    /// </summary>
    public class EnemyMobile : MonoBehaviour
    {
        #region Variables
        public Animator animator;
        private EnemyController enemyController;

        public AIState AIState { get; private set; }

        //이동
        public AudioClip movementSound;
        public MinMaxFloat pitchMovementSpeed;

        private AudioSource audioSource;

        //데미지 - 이펙트
        public ParticleSystem[] randomHitSparks;

        //Detected
        public ParticleSystem[] detectedVfx;
        public AudioClip detectedSfx;

        //attack
        [Range(0f, 1f)]
        public float attackSkipDistanceRatio = 0.5f;

        //animation parameter
        const string k_AnimAttackParameter = "Attack";
        const string k_AnimMoveSpeedParameter = "MoveSpeed";
        const string k_AnimAlertedParameter = "Alerted";
        const string k_AnimOnDamagedParameter = "OnDamaged";
        const string k_AnimDeathParameter = "Death";
        #endregion

        private void Start()
        {
            //참조
            enemyController = GetComponent<EnemyController>();
            enemyController.Damaged += OnDamaged;
            enemyController.OnDetectedTarget += OnDetected;
            enemyController.OnLostTarget += OnLost;
            enemyController.OnAttack += Attacked;

            audioSource = GetComponent<AudioSource>();
            audioSource.clip = movementSound;
            audioSource.Play();

            //초기화
            AIState = AIState.Patrol;
        }

        private void Update()
        {
            //상태 구현
            UpdateAiStateTransition();
            UpdateCurrentAIState();

            //속도에 따른 애니메이션/사운드 효과
            float moveSpeed = enemyController.Agent.velocity.magnitude;
            animator.SetFloat(k_AnimMoveSpeedParameter, moveSpeed);         //애니메이션
            audioSource.pitch = pitchMovementSpeed.GetValueFromRatio(moveSpeed / enemyController.Agent.speed);      //사운드
        }

        //상태에 따른 Enemy 구현
        private void UpdateCurrentAIState()
        {
            switch(AIState)
            {
                case AIState.Patrol:
                    enemyController.UpdatePathDestination(true);
                    enemyController.SetNavDestination(enemyController.GetDestinationPath());
                    break;
                case AIState.Follow:
                    enemyController.SetNavDestination(enemyController.knownDetectedTarget.transform.position);
                    enemyController.OrientToward(enemyController.knownDetectedTarget.transform.position);
                    enemyController.OrientWeaponsToward(enemyController.knownDetectedTarget.transform.position);
                    break;
                case AIState.Attack:
                    //일정 거리까지는 이동하면서 공격
                    float distance = Vector3.Distance(enemyController.knownDetectedTarget.transform.position,
                        enemyController.DetectionModule.detectionSourcePoint.position);

                    if(distance >= enemyController.DetectionModule.attackRange * attackSkipDistanceRatio)   //5이상
                    {
                        enemyController.SetNavDestination(enemyController.knownDetectedTarget.transform.position);  //타겟 위치로 이동
                    }
                    else
                    {
                        enemyController.SetNavDestination(transform.position);  //제자리에 서기
                    }

                    enemyController.OrientToward(enemyController.knownDetectedTarget.transform.position);
                    enemyController.OrientWeaponsToward(enemyController.knownDetectedTarget.transform.position);
                    enemyController.TryAttack(enemyController.knownDetectedTarget.transform.position);
                    break;
            }
        }

        //상태 변경에 따른 구현
        private void UpdateAiStateTransition()
        {
            switch (AIState)
            {
                case AIState.Patrol:
                    break;
                case AIState.Follow:
                    if(enemyController.IsSeeingTarget && enemyController.IsTargetInAttackRange)
                    {
                        AIState = AIState.Attack;
                        enemyController.SetNavDestination(transform.position);  //정지
                    }
                    break;
                case AIState.Attack:
                    if (enemyController.IsTargetInAttackRange == false)
                    {
                        AIState = AIState.Follow;
                    }
                    break;
            }
        }

        private void OnDamaged()
        {
            //스파크 파티클 - 랜덤하게 하나 선택해서 플레이
            if(randomHitSparks.Length > 0)
            {
                int randNum = Random.Range(0, randomHitSparks.Length);
                randomHitSparks[randNum].Play();
            }

            //데미지 애니메이션
            animator.SetTrigger(k_AnimOnDamagedParameter);
        }

        private void OnDetected()
        {
            //상태 변경
            if(AIState == AIState.Patrol)   //공격 중에 추격 방지
            {
                AIState = AIState.Follow;
            }

            //Vfx
            for (int i =0; i < detectedVfx.Length; i++)
            {
                detectedVfx[i].Play();
            }

            //Sfx
            if(detectedSfx)
            {
                AudioUtility.CreateSfx(detectedSfx, this.transform.position, 1f);
            }

            //anim
            animator.SetBool(k_AnimAlertedParameter, true);
        }

        private void OnLost()
        {
            //상태 변경
            if(AIState == AIState.Follow && AIState == AIState.Attack)  //추격이나 공격 상태에서 타겟 잃으면
            {
                AIState = AIState.Patrol;
            }

            //Vfx
            for (int i = 0; i < detectedVfx.Length; i++)
            {
                detectedVfx[i].Stop();
            }

            //anim
            animator.SetBool(k_AnimAlertedParameter, false);
        }

        private void Attacked()
        {
            //애니메이션
            animator.SetTrigger(k_AnimAttackParameter);
        }
    }
}