using Unity.FPS.Game;
using Unity.FPS.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.FPS.UI
{
    public class PlayerHealthBar : MonoBehaviour
    {
        #region Variables
        private Health playerHealth;
        public Image healthFillImage;
        #endregion

        private void Start()
        {
            //참조
            PlayerCharacterController playerCharacterController = GameObject.FindObjectOfType<PlayerCharacterController>();

            playerHealth = playerCharacterController.GetComponent<Health>();
        }

        private void Update()
        {
            healthFillImage.fillAmount = playerHealth.GetRatio();
        }
    }
}