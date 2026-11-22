using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIInputBlocker : MonoBehaviour
{
	static readonly List<UIInputBlocker> m_panels = new List<UIInputBlocker>();
	static int m_blockedFrame = -1;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	static void ResetState()
	{
		m_panels.Clear();
		m_blockedFrame = -1;
	}

	void OnEnable()
	{
		if (!m_panels.Contains(this)) m_panels.Add(this);
		BlockCurrentFrame();
	}

	void OnDisable()
	{
		m_panels.Remove(this);
		BlockCurrentFrame();
	}

	public static void BlockCurrentFrame() { m_blockedFrame = Time.frameCount; }

	public static bool IsBlocked()
	{
		if (m_blockedFrame == Time.frameCount) return true;
		for (int i = m_panels.Count - 1; i >= 0; --i)
		{
			if (m_panels[i] == null) m_panels.RemoveAt(i);
			else if (m_panels[i].isActiveAndEnabled) return true;
		}
		return false;
	}

	public static bool IsGameplayBlocked()
	{
		if (IsBlocked()) return true;
		if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null) return false;
		TMP_InputField input = EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>();
		return input != null && input.isFocused;
	}
}
