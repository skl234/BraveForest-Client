using System;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class ChatTextColorUI : MonoBehaviour
{
	TMP_Text m_text;

	void Awake() { m_text = GetComponent<TMP_Text>(); }
	void OnEnable()
	{
		if (m_text == null) m_text = GetComponent<TMP_Text>();
		m_text.OnPreRenderText += Apply;
	}
	void OnDisable() { if (m_text != null) m_text.OnPreRenderText -= Apply; }

	void Apply(TMP_TextInfo _info)
	{
		string text = m_text.text;
		int start = 0;
		int prefixEnd = 0;
		Color32 color = m_text.color;
		bool newLine = true;
		for (int i = 0; i < _info.characterCount; ++i)
		{
			TMP_CharacterInfo character = _info.characterInfo[i];
			if (newLine)
			{
				color = m_text.color;
				prefixEnd = int.MaxValue;
				if (text.IndexOf("[지역]", start, StringComparison.Ordinal) == start) { color = new Color(0, 1, .89f); prefixEnd = start + 4; }
				else if (text.IndexOf("[전체]", start, StringComparison.Ordinal) == start) { color = new Color(0, 1, .89f); prefixEnd = start + 4; }
				else if (text.IndexOf("[귓속말]", start, StringComparison.Ordinal) == start) color = new Color(.92f, .38f, 1);
				else if (text.IndexOf("[파티]", start, StringComparison.Ordinal) == start) color = new Color(.55f, .79f, 1);
				newLine = false;
			}
			if (character.character == '\n') { start = character.index + 1; newLine = true; continue; }
			if (!character.isVisible) continue;
			Color32 value = color;
			if (character.index >= prefixEnd) value = m_text.color;
			Color32[] colors = _info.meshInfo[character.materialReferenceIndex].colors32;
			for (int j = 0; j < 4; ++j) colors[character.vertexIndex + j] = value;
		}
	}
}
