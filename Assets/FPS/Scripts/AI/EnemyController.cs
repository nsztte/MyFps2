using UnityEngine;
using Unity.FPS.Game;

namespace Unity.FPS.AI
{
    public class EnemyController : MonoBehaviour
    {
        #region Variables
        private Health health;

        //죽음
        public GameObject deathVfxPrefab;
        public Transform deathVfxSpawnPosition;
        #endregion

        private void Start()
        {
            //참조
            health = GetComponent<Health>();

            health.OnDamaged += OnDamaged;
            health.OnDie += OnDie;
        }

        private void OnDamaged(float damage, GameObject damageSource)
        {

        }

        private void OnDie()
        {
            //폭발 효과
            GameObject vfxGo = Instantiate(deathVfxPrefab, deathVfxSpawnPosition.position, Quaternion.identity);
            Destroy(vfxGo, 5f);

            //Enemy 킬
            Destroy(gameObject);
        }
    }
}