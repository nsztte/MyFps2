using UnityEngine;

namespace Unity.FPS.Game
{
    /// <summary>
    /// 오디오 플레이 관련 기능 구현
    /// </summary>
    public class AudioUtility : MonoBehaviour
    {
        //지정된 위치에 게임 오브젝트 생성하고 AudioSource 컴포넌트를 추가해서 지정된 클립을 플레이
        //클립 사운드 플레이가 끝나면 자동으로 킬 - TimeSelfDestruction 컴포넌트 이용
        public static void CreateSfx(AudioClip clip, Vector3 position, float spartialBlend, float rolloffDistanceMin = 1)
        {
            GameObject impactInstance = new GameObject();   //빈 오브젝트 생성
            impactInstance.transform.position = position;

            //audio clip play
            AudioSource source = impactInstance.AddComponent<AudioSource>();    //AudioSource 컴포넌트 추가
            source.clip = clip;
            source.spatialBlend = spartialBlend;
            source.minDistance = rolloffDistanceMin;
            source.Play();

            //오브젝트 kill
            TimeSelfDestruct timeselfDestruct = impactInstance.AddComponent<TimeSelfDestruct>();
            timeselfDestruct.lifeTime = clip.length;    //클립의 플레이타임
        }
    }
}