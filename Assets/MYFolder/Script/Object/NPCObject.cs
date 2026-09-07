using UnityEngine;

public class NPCObject : MonoBehaviour
{
	[SerializeField] Animator m_animator;
	[SerializeField] string m_idleAnimation = "Idle";
	[SerializeField] ChatBubbleUI m_chatBubble;
	[SerializeField] string m_dialogue = "이 앞은 위험하네. 단단히 준비를 하게나.";

	void Start()
	{
		if (m_chatBubble != null) m_chatBubble.ShowPermanent(m_dialogue);
		if (m_animator != null) m_animator.Play(m_idleAnimation, 0, 0.0f);
	}
}
