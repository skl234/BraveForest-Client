using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum eUIStyle
{
	Panel,
	Inset,
	Button,
	Input,
	Overlay,
}

// 공통 색상은 테마에서 가져옴. 적용된 결과는 프리팹/씬에 미리 저장해둠
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public class UIStyle : MonoBehaviour
{
	[SerializeField] UITheme		m_theme;
	[SerializeField] eUIStyle	m_style;

	public UITheme GetTheme() { return m_theme; }
	public eUIStyle GetStyle() { return m_style; }

	public void Setup(UITheme _theme, eUIStyle _style)
	{
		m_theme = _theme;
		m_style = _style;
		Apply();
	}

	public void Apply()
	{
		if (m_theme == null) return;
		Image image = GetComponent<Image>();
		image.sprite = m_theme.panelSprite;
		image.type = Image.Type.Sliced;
		image.pixelsPerUnitMultiplier = 2.0f;
		image.color = m_theme.panelColor;
		if (m_style == eUIStyle.Inset) image.color = m_theme.insetColor;
		if (m_style == eUIStyle.Overlay)
		{
			image.sprite = null;
			image.type = Image.Type.Simple;
			image.color = m_theme.overlayColor;
		}

		Outline outline = GetComponent<Outline>();
		if (outline != null)
		{
			outline.effectColor = m_theme.borderColor;
			outline.effectDistance = new Vector2(1, -1);
			outline.enabled = m_style != eUIStyle.Overlay;
		}
		if (m_style == eUIStyle.Button)
		{
			image.sprite = m_theme.buttonSprite;
			image.color = m_theme.buttonColor;
			Button button = GetComponent<Button>();
			if (button != null)
			{
				button.targetGraphic = image;
				button.transition = Selectable.Transition.ColorTint;
				button.colors = m_theme.GetButtonColors();
			}
			TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
			for (int i = 0; i < texts.Length; ++i) texts[i].color = m_theme.buttonTextColor;
		}
		if (m_style == eUIStyle.Input)
		{
			image.color = m_theme.inputColor;
			TMP_InputField input = GetComponent<TMP_InputField>();
			if (input != null)
			{
				input.textComponent.color = m_theme.buttonTextColor;
				if (input.placeholder != null) input.placeholder.color = m_theme.placeholderColor;
				input.customCaretColor = true;
				input.caretColor = m_theme.buttonTextColor;
			}
		}
	}
}
