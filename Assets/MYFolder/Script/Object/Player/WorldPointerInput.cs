using UnityEngine;
using UnityEngine.EventSystems;

// 포인터 입력만 담당합니다. 전투 판정/경로 계산/시각 효과 구현은 별도 컴포넌트에 위임합니다.
[RequireComponent(typeof(PlayerCombat))]
public class WorldPointerInput : MonoBehaviour
{
	[SerializeField] float m_commandInterval = 0.2f;
	[SerializeField] LayerMask m_worldLayers = (1 << 0) | (1 << 3) | (1 << 7);
	[SerializeField] LayerMask m_groundLayers = 1 << 3;
	[SerializeField] DestinationIndicator m_destinationPrefab;
	[SerializeField] RPGCursor m_cursor;
	PlayerCombat m_combat;
	float m_nextCommand;
	DestinationIndicator m_indicator;
	public Vector3 LastDestination { get; private set; }
	public int MoveCommandCount { get; private set; }
	void Awake() { m_combat = GetComponent<PlayerCombat>(); }

	public void Tick()
	{
		if (UIInputBlocker.IsGameplayBlocked()) return;
		if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null &&
			EventSystem.current.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>() != null) return;
		if (Input.GetKeyDown(KeyCode.A)) m_combat.TryHeavy();
		if (Input.GetKeyDown(KeyCode.S)) m_combat.TryArea();
		if (Input.GetKeyDown(KeyCode.D)) m_combat.TryPotion();
		bool hit = false;
		MonsterHitbox monster = null;
		Vector3 point = default;
		if (Camera.main != null) hit = TryResolvePointer(Camera.main.ScreenPointToRay(Input.mousePosition), out monster, out point);
		if (m_cursor != null) m_cursor.SetTargetable(monster != null);
		if (!hit) return;
		if (Input.GetMouseButtonDown(0)) SubmitLeft(monster);
		else if (Input.GetMouseButton(1)) SubmitRight(monster, point, Time.unscaledTime);
	}

	public bool TryResolvePointer(Ray _ray, out MonsterHitbox _monster, out Vector3 _point)
	{
		_monster = null;
		_point = default;
		if (UIInputBlocker.IsGameplayBlocked()) return false;
		if (EventSystem.current != null)
		{
			if (EventSystem.current.IsPointerOverGameObject()) return false;
			GameObject selected = EventSystem.current.currentSelectedGameObject;
			if (selected != null && selected.GetComponent<TMPro.TMP_InputField>() != null) return false;
		}
		return WorldPointerRaycaster.TryResolve(_ray, m_worldLayers, m_groundLayers, out _monster, out _point);
	}

	public void SubmitLeft(MonsterHitbox _monster)
	{
		if (_monster != null && _monster.IsAlive())
		{
			HideDestination();
			m_combat.CommandTarget(_monster, true);
		}
		else if (m_combat.GetTarget() != null) m_combat.ClearTarget();
	}

	// 클릭을 떼었다 다시 눌러도 타이머를 초기화하지 않으므로 연타로 제한을 우회할 수 없습니다.
	public bool SubmitRight(MonsterHitbox _monster, Vector3 _point, float _now)
	{
		if (_now + 0.00001f < m_nextCommand) return false;
		m_nextCommand = _now + m_commandInterval;
		if (_monster != null && _monster.IsAlive())
		{
			HideDestination();
			m_combat.CommandTarget(_monster, true);
			return true;
		}
		if (!m_combat.TryCommandMove(_point, out Vector3 destination)) return false;
		LastDestination = destination;
		++MoveCommandCount;
		if (m_destinationPrefab != null)
		{
			HideDestination();
			m_indicator = Instantiate(m_destinationPrefab, destination + Vector3.up * 0.06f, Quaternion.identity);
		}
		return true;
	}
	void HideDestination() { if (m_indicator != null) Destroy(m_indicator.gameObject); }
	void OnDisable() { HideDestination(); if (m_cursor != null) m_cursor.SetTargetable(false); }
}
