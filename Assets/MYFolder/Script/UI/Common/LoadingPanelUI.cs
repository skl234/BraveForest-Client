using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingPanelUI : MonoBehaviour
{
	[SerializeField] GameObject m_panel;
	[SerializeField] TMP_Text m_text;
	static LoadingPanelUI m_instance;
	bool m_loadingScene;
	bool m_ready;
	float m_started;
	Vector2 m_textPosition;

	public static bool IsVisible() { return m_instance != null && m_instance.m_panel.activeSelf; }

	void Awake()
	{
		if (m_instance != null && m_instance != this)
		{
			Destroy(gameObject);
			return;
		}
		m_instance = this;
		m_textPosition = m_text.rectTransform.anchoredPosition;
		DontDestroyOnLoad(gameObject);
		m_panel.SetActive(false);
	}

	void OnDestroy() { if (m_instance == this) m_instance = null; }

	public static void Show()
	{
		if (m_instance == null) return;
		m_instance.m_started = Time.unscaledTime;
		m_instance.m_panel.SetActive(true);
	}

	public static void Hide()
	{
		if (m_instance == null) return;
		if (m_instance.m_loadingScene)
		{
			m_instance.m_ready = true;
			return;
		}
		m_instance.m_panel.SetActive(false);
	}

	public static void ChangeScene(string _sceneName)
	{
		if (m_instance == null)
		{
			SceneManager.LoadSceneAsync(_sceneName);
			return;
		}
		if (m_instance.m_loadingScene) return;
		m_instance.m_loadingScene = true;
		m_instance.m_ready = false;
		Show();
		m_instance.StartCoroutine(m_instance.LoadScene(_sceneName));
	}

	IEnumerator LoadScene(string _sceneName)
	{
		// 로딩 화면을 먼저 그린 뒤 씬을 읽음
		yield return null;
		yield return SceneManager.LoadSceneAsync(_sceneName);
		m_loadingScene = false;
		if (m_ready) Hide();
	}

	void Update()
	{
		if (!m_panel.activeSelf) return;
		int dots = (int)((Time.unscaledTime - m_started) * 2) % 4;
		m_text.text = "LOADING" + new string('.', dots);
		m_text.rectTransform.anchoredPosition = m_textPosition + new Vector2(0, Mathf.Sin((Time.unscaledTime - m_started) * 3) * 3);
	}
}
