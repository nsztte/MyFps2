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

        //Sfx
        public AudioClip movementSound;
        public MinMaxFloat pitchMovementSpeed;

        private AudioSource audioSource;

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

            audioSource = GetComponent<AudioSource>();
            audioSource.clip = movementSound;
            audioSource.Play();

            //초기화
            AIState = AIState.Patrol;
        }

        private void Update()
        {
            UpdateCurrentAIState();
        }

        //상태에 따른 Enemy 구현
        private void UpdateCurrentAIState()
        {
            switch(AIState)
            {
                case AIState.Patrol:
                    enemyController.UpdatePathDestination();    //true..?
                    enemyController.SetNavDestination(enemyController.GetDestinationPath());
                    break;
                case AIState.Follow:
                    break;
                case AIState.Attack:
                    break;
            }
        }

        private void OnDamaged()
        {

        }
    }
}