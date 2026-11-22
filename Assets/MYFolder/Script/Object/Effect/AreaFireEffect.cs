using UnityEngine;

// 원형으로 미리 배치된 ParticleSystem 자식들을 가진 독립 프리팹입니다.
public class AreaFireEffect : MonoBehaviour
{
	[SerializeField] float m_lifetime = 3.0f;
	void Start()
	{
		Destroy(gameObject, m_lifetime);
	}
}
