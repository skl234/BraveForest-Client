using UnityEngine;
using UnityEngine.AI;
using TMPro;

public class OfflineCombatSession : MonoBehaviour
{
    [SerializeField] LocalPlayerObject m_warrior;
    [SerializeField] LocalPlayerObject m_archer;
    [SerializeField] MonsterHitbox[] m_monsters;
    [SerializeField] GameObject m_selectionPanel;
    [SerializeField] CombatHUD m_hud;
    [SerializeField] PlayerFollowCamera m_camera;
    [SerializeField] TMP_Text m_monsterMovementLabel;
    Vector3 m_spawn;
    Vector3[] m_monsterPositions;
    LocalPlayerObject m_player;
    bool m_monstersMoving;
    public LocalPlayerObject GetPlayer() { return m_player; }
    public bool AreMonstersMoving() { return m_monstersMoving; }
    void Awake()
    {
        m_spawn = m_warrior.transform.position;
        m_monsterPositions = new Vector3[m_monsters.Length];
        for (int i = 0; i < m_monsters.Length; ++i) m_monsterPositions[i] = m_monsters[i].transform.parent.position;
        UpdateMonsterMovementLabel();
    }
    public void ChooseWarrior() { Begin(m_warrior); }
    public void ChooseArcher() { Begin(m_archer); }
    void Begin(LocalPlayerObject _player)
    {
        m_player = _player;
        m_player.gameObject.SetActive(true);
        m_player.GetComponent<NavMeshAgent>().Warp(m_spawn);
        m_player.transform.position = m_spawn;
        for (int i = 0; i < m_monsters.Length; ++i) m_monsters[i].transform.parent.gameObject.SetActive(true);
        ResetMonsters();
        SetMonsterMovement(false);
        m_camera.SetTarget(m_player.transform);
        m_hud.gameObject.SetActive(true);
        m_hud.Bind(m_player);
        m_selectionPanel.SetActive(false);
    }
    public void OpenSelection()
    {
        if (m_player != null)
        {
            m_player.GetComponent<PlayerCombat>().StopCombat();
            m_player.gameObject.SetActive(false);
        }
        m_player = null;
        for (int i = 0; i < m_monsters.Length; ++i) m_monsters[i].transform.parent.gameObject.SetActive(false);
        m_hud.gameObject.SetActive(false);
        m_selectionPanel.SetActive(true);
    }
    public void SkillA() { if (m_player != null) m_player.GetComponent<PlayerCombat>().TryHeavy(); }
    public void SkillS() { if (m_player != null) m_player.GetComponent<PlayerCombat>().TryArea(); }
	public void UsePotion() { if (m_player != null) m_player.GetComponent<PlayerCombat>().TryPotion(); }
    public void BasicAttack()
    {
        if (m_player == null) return;
        PlayerCombat combat = m_player.GetComponent<PlayerCombat>();
        combat.CommandTarget(combat.GetTarget(), true);
    }
    public void KillTarget()
    {
        if (m_player == null) return;
        MonsterHitbox target = m_player.GetComponent<PlayerCombat>().GetTarget();
        if (target != null) target.SetDead();
    }
    public void ResetMonsters()
    {
        if (m_player != null) m_player.GetComponent<PlayerCombat>().StopCombat();
        for (int i = 0; i < m_monsters.Length; ++i)
        {
            MonsterWander wander = m_monsters[i].GetComponentInParent<MonsterWander>();
            if (wander != null) wander.ResetOrigin(m_monsterPositions[i]);
            else m_monsters[i].transform.parent.position = m_monsterPositions[i];
            m_monsters[i].ResetTest();
            if (wander != null) wander.SetWandering(m_monstersMoving);
        }
    }

    public void ToggleMonsterMovement() { SetMonsterMovement(!m_monstersMoving); }

    void SetMonsterMovement(bool _moving)
    {
        m_monstersMoving = _moving;
        for (int i = 0; i < m_monsters.Length; ++i)
        {
            MonsterWander wander = m_monsters[i].GetComponentInParent<MonsterWander>();
            if (wander != null) wander.SetWandering(_moving);
        }
        UpdateMonsterMovementLabel();
    }

    void UpdateMonsterMovementLabel()
    {
        if (m_monsterMovementLabel != null) m_monsterMovementLabel.text = m_monstersMoving ? "몬스터 정지" : "몬스터 이동";
    }
}
