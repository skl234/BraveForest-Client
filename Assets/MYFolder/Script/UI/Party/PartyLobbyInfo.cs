using System.Collections.Generic;

public class PartyListingInfo
{
	public ulong partyNum;
	public ulong createNum;
	public string name;
	public string leaderName;
	public readonly List<PartyMemberInfo> members = new List<PartyMemberInfo>();
}

public class PartyApplicationInfo
{
	public ulong requestNum;
	public string name;
	public ulong level;
	public ulong jobClass;
}

public interface IPartyLobbyRequests
{
	bool Refresh(ulong _page);
	bool Create(string _name);
	bool Join(ulong _partyNum);
	bool Cancel(ulong _partyNum);
	bool Answer(ulong _requestNum, bool _accept);
}

public class PartyLobbyInfo
{
	readonly List<PartyListingInfo> m_parties = new List<PartyListingInfo>();
	readonly List<PartyApplicationInfo> m_applications = new List<PartyApplicationInfo>();
	readonly HashSet<ulong> m_knownRequests = new HashSet<ulong>();
	public string message = "";
	public ulong messageRequestNum { get; private set; }
	public bool listPending { get; private set; }
	public ulong page { get; private set; }
	public ulong totalCount { get; private set; }
	public ulong requestedPage { get; private set; }
	public ulong requestedParty { get; private set; }
	public ulong requestNum { get; private set; }
	public uint revision { get; private set; }

	public IReadOnlyList<PartyListingInfo> GetParties() { return m_parties; }
	public IReadOnlyList<PartyApplicationInfo> GetApplications() { return m_applications; }
	public PartyApplicationInfo FindApplication(ulong _requestNum)
	{
		for (int i = 0; i < m_applications.Count; ++i)
		{
			if (m_applications[i].requestNum == _requestNum) return m_applications[i];
		}
		return null;
	}
	public PartyApplicationInfo TakeNewApplication()
	{
		PartyApplicationInfo latest = null;
		for (int i = 0; i < m_applications.Count; ++i)
		{
			if (m_knownRequests.Add(m_applications[i].requestNum)) latest = m_applications[i];
		}
		return latest;
	}
	public void SetParties(IList<PartyListingInfo> _list, ulong _page = 0, ulong _totalCount = 0)
	{
		m_parties.Clear();
		if (_list != null) m_parties.AddRange(_list);
		page = _page;
		totalCount = _totalCount;
		if (_totalCount == 0) totalCount = (ulong)m_parties.Count;
		CompleteList("");
	}
	public void SetApplications(IList<PartyApplicationInfo> _list)
	{
		m_applications.Clear();
		if (_list != null) m_applications.AddRange(_list);
		m_knownRequests.RemoveWhere(number => FindApplication(number) == null);
		++revision;
	}
	public void ClearApplications()
	{
		m_applications.Clear();
		m_knownRequests.Clear();
		++revision;
	}
	public void Complete(string _message, ulong _requestNum = 0) { message = _message; messageRequestNum = _requestNum; ++revision; }
	public void CompleteList(string _message) { listPending = false; Complete(_message); }
	public void SetRequestedParty(ulong _partyNum, ulong _requestNum = 0)
	{
		requestedParty = _partyNum;
		requestNum = _requestNum;
		++revision;
	}
	public void BeginListRequest(ulong _page)
	{
		requestedPage = _page;
		listPending = true;
		Complete("목록을 불러오는 중...");
	}
	public void ClearRequests()
	{
		ClearApplications();
		requestedParty = 0;
		requestNum = 0;
		Complete("");
	}
	public void Clear()
	{
		m_parties.Clear();
		page = 0;
		totalCount = 0;
		requestedPage = 0;
		listPending = false;
		ClearRequests();
	}
}
