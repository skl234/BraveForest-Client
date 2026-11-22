using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ZoneChannelRowUI : MonoBehaviour
{
	[SerializeField] TMP_Text m_label;
	[SerializeField] Button m_button;
	ZoneChannelUI m_owner;
	ulong m_channelId;

	public void Bind(ZoneChannelUI _owner, ulong _channelId, ulong _count, ulong _maxCount, bool _available, bool _current)
	{
		m_owner = _owner;
		m_channelId = _channelId;
		m_label.text = "채널 " + _channelId + "    " + _count + " / " + _maxCount;
		if (_current) m_label.text += "    현재";
		m_button.interactable = _available && !_current && _count < _maxCount;
	}
	public void Select() { if (m_owner != null) m_owner.ChangeChannel(m_channelId); }
}

