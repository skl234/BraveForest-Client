using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ZoneChannelUI : MonoBehaviour
{
	[SerializeField] TMP_Text m_currentChannel;
	[SerializeField] GameObject m_panel;
	[SerializeField] TMP_Text m_message;
	[SerializeField] ZoneChannelRowUI[] m_rows;
	[SerializeField] ScrollRect m_scrollRect;

	public void SetChannel(ulong _channelId) { m_currentChannel.text = "채널" + _channelId; }
	public void Open()
	{
		if (UIInputBlocker.IsBlocked()) return;
		ZoneMapUI map = FindObjectOfType<ZoneMapUI>();
		if (map != null) map.Collapse();
		m_panel.SetActive(true);
		m_message.text = "채널 정보를 불러오는 중...";
		for (int i = 0; i < m_rows.Length; ++i) m_rows[i].gameObject.SetActive(false);
		if (ZoneSceneNetwork.Instance == null || !ZoneSceneNetwork.Instance.RequestChannelList())
			m_message.text = "서버와 연결되어 있지 않습니다.";
	}
	public void Close() { m_panel.SetActive(false); }
	public void ChangeChannel(ulong _channelId)
	{
		if (ZoneSceneNetwork.Instance == null || !ZoneSceneNetwork.Instance.RequestChangeChannel(_channelId)) return;
		m_message.text = "채널 이동 중...";
	}
	public void ShowResult(bool _success)
	{
		if (_success) Close();
		else m_message.text = "채널을 이동할 수 없는 상태입니다.";
	}
	public void SetList(ChannelInfo[] _list, ulong _currentChannel)
	{
		if (_list == null) return;
		m_message.text = "조회 시점의 인원입니다. 이동할 채널을 선택하세요.";
		for (int i = 0; i < m_rows.Length; ++i)
		{
			m_rows[i].gameObject.SetActive(i < _list.Length);
			if (i >= _list.Length) continue;
			ChannelInfo info = _list[i];
			m_rows[i].Bind(this, info.channelId, info.playerCount, info.maxPlayerCount, info.available, info.channelId == _currentChannel);
		}
		if (m_scrollRect != null)
		{
			// 표시한 채널 수로 높이를 다시 계산. 이전 목록의 빈 영역에 머무르지 않게
			LayoutRebuilder.ForceRebuildLayoutImmediate(m_scrollRect.content);
			m_scrollRect.StopMovement();
			m_scrollRect.verticalNormalizedPosition = 1;
		}
	}
}
