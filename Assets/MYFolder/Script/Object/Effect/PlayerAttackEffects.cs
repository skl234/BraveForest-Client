using UnityEngine;

// 로컬/원격 플레이어가 공유하는 발사·범위기 연출. 피해와 HP는 여기서 처리하지 않음
public class PlayerAttackEffects : MonoBehaviour
{
	[SerializeField] ArrowShotEffect m_arrowBasic;
	[SerializeField] ArrowShotEffect m_arrowPower;
	[SerializeField] ArrowShotEffect m_arrowRapid;
	[SerializeField] Transform m_arrowFirePoint;
	[SerializeField] GameObject m_areaEffectPrefab;

	public void PlayArrow(byte _attackType, Vector3 _target)
	{
		ArrowShotEffect prefab = m_arrowBasic;
		if (_attackType == 2) prefab = m_arrowPower;
		if (_attackType == 4) prefab = m_arrowRapid;
		if (prefab == null) return;
		Instantiate(prefab).Initialize(GetArrowStart(_target), _target);
	}

	public void PlayArea(float _radius)
	{
		if (m_areaEffectPrefab == null) return;
		GameObject effect = Instantiate(m_areaEffectPrefab, transform.position, Quaternion.identity);
		effect.transform.localScale *= _radius / 5.0f;
	}

	public Vector3 GetArrowStart(Vector3 _target)
	{
		if (m_arrowFirePoint == null || !m_arrowFirePoint.gameObject.activeInHierarchy)
		{
			Transform[] transforms = GetComponentsInChildren<Transform>(true);
			for (int i = 0; i < transforms.Length; ++i)
			{
				if (transforms[i].name != "Arrow1" && transforms[i].name != "Arrow2") continue;
				if (!transforms[i].gameObject.activeInHierarchy) continue;
				m_arrowFirePoint = transforms[i];
				break;
			}
		}
		if (m_arrowFirePoint == null) return transform.position + Vector3.up * 1.3f + transform.forward * .4f;
		Renderer[] renderers = m_arrowFirePoint.GetComponentsInChildren<Renderer>(true);
		if (renderers.Length == 0) return m_arrowFirePoint.position;
		Bounds bounds = renderers[0].bounds;
		for (int i = 1; i < renderers.Length; ++i) bounds.Encapsulate(renderers[i].bounds);
		return bounds.ClosestPoint(_target);
	}
}
