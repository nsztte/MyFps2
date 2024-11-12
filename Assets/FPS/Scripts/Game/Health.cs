using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.FPS.Game
{
    /// <summary>
    /// 체력을 관리하는 클래스
    /// </summary>
    public class Health : MonoBehaviour
    {
        #region Variables
        [SerializeField] private float maxHealth = 100f;    //최대 Hp
        public float CurrentHealth { get; private set; }    //현재 Hp
        public bool isDeath = false;                       //죽음 체크

        public UnityAction<float, GameObject> OnDamaged;    //데미지
        public UnityAction OnDie;                           //죽음
        public UnityAction<float> OnHeal;                   //힐

        //체력 위험 경계율
        [SerializeField] private float criticalHealthRatio = 0.3f;

        //무적
        public bool Invincible { get; private set; }
        #endregion

        //힐 아이템을 먹을 수 있는지 체크
        public bool CanPickup() => CurrentHealth < maxHealth;
        //UI HP 게이지 값
        public float GetRatio() => CurrentHealth / maxHealth;
        //위험 체크
        public bool IsCritical() => GetRatio() < criticalHealthRatio;


        private void Start()
        {
            //초기화
            CurrentHealth = maxHealth;
            Invincible = false;
        }

        //힐
        public void TakeHeal(float amount)
        {
            float beforeHealth = CurrentHealth;
            CurrentHealth += amount;
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);

            //real Heal 구하기
            float realHeal = CurrentHealth - beforeHealth;
            if (realHeal > 0)
            {
                //힐 구현
                OnHeal?.Invoke(realHeal);
            }
        }

        //damageSource: 데미지 주는 주체
        public void TakeDamage(float damage, GameObject damageSource)
        {
            //무적 체크
            if (Invincible)
                return;

            Debug.Log($"CurrentHealth: {CurrentHealth}");

            float beforeHealth = CurrentHealth;
            CurrentHealth -= damage;
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0, maxHealth);

            //real Damage 구하기
            float realDamage = beforeHealth - CurrentHealth;
            if (realDamage > 0)
            {
                //데미지 구현
                OnDamaged?.Invoke(realDamage, damageSource);    //if(OnDamaged != null)
            }

            //죽음 처리
            HandleDeath();
        }

        //죽음 처리 관리
        void HandleDeath()
        {
            //죽음 체크
            if (isDeath)
                return;

            if (CurrentHealth <= 0)
            {
                isDeath = true;

                //죽음 구현
                OnDie?.Invoke();    //딜리게이트 함수 호출
            }
        }
    }
}
