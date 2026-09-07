using UnityEngine;
using System.Collections;

// 숫자/피격 효과만 담당. 플레이어와 몬스터 모두 같은 프리팹 구성으로 사용
public class CombatHitFeedback : MonoBehaviour
{
    [SerializeField] GameObject m_hitEffectPrefab;
    [SerializeField] DamageNumberEffect m_damageNumberPrefab;
    [SerializeField] Color m_damageColor = new Color(1, 0.82f, 0.18f, 1);
    [SerializeField] float m_hitHeight = 1.1f;
    [SerializeField] float m_numberHeight = 2.7f;
    [SerializeField] float m_hitScale = 1.35f;
    [SerializeField] float m_cameraOffset = 0.25f;
    int m_numberIndex;
    float m_lastHitTime;

    public float GetPendingHitDelay() { return Mathf.Max(0, m_lastHitTime - Time.time); }

    public void PlayDelayed(int _damage, float _delay)
    {
        if (_damage <= 0) return;
        if (_delay <= 0) { Play(_damage); return; }
        m_lastHitTime = Mathf.Max(m_lastHitTime, Time.time + _delay);
        StartCoroutine(PlayAfter(_damage, _delay));
    }

    IEnumerator PlayAfter(int _damage, float _delay)
    {
        yield return new WaitForSeconds(_delay);
        Play(_damage);
    }

    void OnDisable()
    {
        StopAllCoroutines();
        m_lastHitTime = 0;
    }

    public void Play(int _damage)
    {
        if (_damage <= 0) return;
        if (m_hitEffectPrefab != null)
        {
            Vector3 position = transform.position + Vector3.up * m_hitHeight;
            if (Camera.main != null) position += (Camera.main.transform.position - position).normalized * m_cameraOffset;
            GameObject effect = Instantiate(m_hitEffectPrefab, position, Quaternion.identity);
            // 원본 Hit_02의 크기는 3. 1.35로 덮어쓰면 오히려 작아지므로 원본 배율을 유지
            effect.transform.localScale *= m_hitScale;
            effect.transform.SetParent(transform, true);
            Destroy(effect, 2.0f);
        }
        if (m_damageNumberPrefab != null)
        {
            Vector3 right = Vector3.right;
            if (Camera.main != null) right = Camera.main.transform.right;
            float offset = (m_numberIndex % 3 - 1) * 0.28f;
            ++m_numberIndex;
            DamageNumberEffect number = Instantiate(m_damageNumberPrefab, transform.position + Vector3.up * m_numberHeight + right * offset, Quaternion.identity);
            number.Play(_damage, m_damageColor);
        }
    }
}
