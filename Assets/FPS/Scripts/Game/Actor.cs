using UnityEngine;

namespace Unity.FPS.Game
{
    /// <summary>
    /// 게임에 등장하는 Actor
    /// </summary>
    public class Actor : MonoBehaviour
    {
        #region Variables
        //소속 - 아군 적군 구분
        public int affiliation;
        //조준점
        public Transform aimPoint;

        private ActorManager actorManager;
        #endregion

        private void Start()
        {
            //Actor 리스트에 추가(등록)
            actorManager = GameObject.FindObjectOfType<ActorManager>();
            //리스트에 포함되어 있는지 여부를 체크
            if(actorManager.Actors.Contains(this) == false)
            {
                actorManager.Actors.Add(this);
            }
        }

        //오브젝트가 킬될때 호출되는 함수
        private void OnDestroy()
        {
            //Actors 리스트에서 삭제
            if(actorManager)
            {
                actorManager.Actors.Remove(this);
            }
        }        
    }
}