using UnityEngine;

namespace MMO
{
    public class Object : MonoBehaviour
    {
        protected Animator m_animator;

        protected virtual void Awake()
        {
            m_animator = GetComponentInChildren<Animator>();
            if (m_animator != null) m_animator.applyRootMotion = false;
        }

        public Animator GetAnimator()
        {
            return m_animator;
        }
    }
}
