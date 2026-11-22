using UnityEngine;

public static class GameScreen
{
	const string ResolutionKey = "Display.Resolution";
	static int m_resolutionIndex;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	static void Initialize()
	{
		m_resolutionIndex = PlayerPrefs.GetInt(ResolutionKey, 0);
		if (m_resolutionIndex < 0 || m_resolutionIndex > 1) m_resolutionIndex = 0;
		Apply();
	}

	public static int GetResolutionIndex()
	{
		return m_resolutionIndex;
	}

	public static Vector2Int GetResolution(int _index)
	{
		if (_index == 1) return new Vector2Int(1280, 720);
		return new Vector2Int(1920, 1080);
	}

	public static bool SetResolution(int _index)
	{
		if (_index < 0 || _index > 1) return false;
		m_resolutionIndex = _index;
		Apply();
		PlayerPrefs.SetInt(ResolutionKey, m_resolutionIndex);
		PlayerPrefs.Save();
		return true;
	}

	static void Apply()
	{
#if !UNITY_EDITOR
		Vector2Int size = GetResolution(m_resolutionIndex);
		Screen.SetResolution(size.x, size.y, FullScreenMode.Windowed);
#endif
	}
}
