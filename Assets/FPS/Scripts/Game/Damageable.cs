using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.FPS.Game
{
    /// <summary>
    /// 데미지를 입는 충돌체(hit box)에 부착되어 데미지를 관리하는 클래스
    /// </summary>
    public class Damageable : MonoBehaviour
    {
        #region Variables
        private Health health;

        //데미지 계수
        [SerializeField] private float damageMultiplier = 1f;

        //스스로 입힌 데미지 계수
        [SerializeField] private float sensibilityToSelfDamage = 0.5f;
        #endregion

        private void Awake()
        {
            //참조
            health = GetComponent<Health>();

            if(health == null)  //health가 없는 히트박스면 그 충돌체의 부모(player) health를 찾음
            {
                health = GetComponentInParent<Health>();
            }
        }

        public void InflictDamage(float damage, bool isExplosionDamage, GameObject damageSource)
        {
            if(health == null)
                return;

            //totalDamage가 실제 데미지값
            var totalDamage = damage;

            //폭발 데미지 체크 - 폭발 데미지일때는 damageMultiplier 계산하지 않음
            if(!isExplosionDamage)
            {
                totalDamage *= damageMultiplier;
            }

            //스스로 입힌 데미지면
            if(health.gameObject == damageSource)
            {
                totalDamage *= sensibilityToSelfDamage;
            }

            //데미지 입히기
            health.TakeDamage(totalDamage, damageSource);
        }
    }
}