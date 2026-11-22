using UnityEngine;

public static class WorldPointerRaycaster
{
	const float MAX_DISTANCE = 2000;

	public static bool TryResolve(Ray _ray, LayerMask _worldLayers, LayerMask _groundLayers, out MonsterHitbox _monster, out Vector3 _point)
	{
		_monster = null;
		_point = default;
		// 대상 선택은 가장 먼저 보이는 오브젝트만 확인.
		if (Physics.Raycast(_ray, out RaycastHit hit, MAX_DISTANCE, _worldLayers, QueryTriggerInteraction.Collide))
		{
			MonsterHitbox target = hit.collider.GetComponent<MonsterHitbox>();
			if (target != null && target.IsAlive())
			{
				_monster = target;
				_point = hit.point;
				return true;
			}
		}

		// 이동 좌표만 바닥에서 찾음. 장애물 우회는 기존 경로 계산에서 처리.
		if (!Physics.Raycast(_ray, out hit, MAX_DISTANCE, _groundLayers, QueryTriggerInteraction.Ignore)) return false;
		_point = hit.point;
		return true;
	}
}
