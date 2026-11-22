using UnityEngine;
using System.Collections.Generic;

public class CharacterPreview : MonoBehaviour
{
	[SerializeField] Camera m_camera;
	RenderTexture m_texture;
	AnimatorOverrideController m_idleController;

	public RenderTexture GetTexture() { return m_texture; }

	public RenderTexture Initialize(GameObject _prefab, ulong _jobClass, int _slotIndex)
	{
		GameObject character = Instantiate(_prefab, transform);
		character.name = _prefab.name;
		character.transform.localPosition = Vector3.zero;
		character.transform.localRotation = Quaternion.identity;

		string idleState = "Idle_SwordShield";
		if (_jobClass != 0) idleState = "Idle_Bow";
		Animator animator = character.GetComponentInChildren<Animator>();
		PlayerVisual visual = character.GetComponent<PlayerVisual>();
		if (visual != null)
		{
			animator = visual.GetAnimator();
			idleState = visual.GetAnimationData().GetIdleState();
		}
		if (animator != null)
		{
			animator.applyRootMotion = false;
			if (visual != null)
			{
				visual.Play(idleState);
				animator.Update(0);
			}
			else PlayIdleOnly(animator, idleState);
		}

		Renderer[] renderers = character.GetComponentsInChildren<Renderer>(true);
		if (renderers.Length == 0)
		{
			Debug.LogError("캐릭터 프리팹에서 Renderer를 찾지 못했습니다: " + _prefab.name);
			return null;
		}

		for (int i = 0; i < renderers.Length; ++i)
		{
			if (!renderers[i].gameObject.activeInHierarchy) continue;

			renderers[i].enabled = true;

			SkinnedMeshRenderer skinnedMeshRenderer = renderers[i] as SkinnedMeshRenderer;
			if (skinnedMeshRenderer != null) skinnedMeshRenderer.updateWhenOffscreen = true;
		}

		m_texture = new RenderTexture(600, 736, 24, RenderTextureFormat.ARGB32);
		m_texture.name = "Character_Slot_" + (_slotIndex + 1) + "_Texture";
		m_texture.antiAliasing = 4;
		m_texture.Create();

		m_camera.targetTexture = m_texture;
		m_camera.Render();
		FitPreview(m_camera, m_texture);
		m_camera.Render();
		return m_texture;
	}
	static void FitPreview(Camera _camera, RenderTexture _texture)
	{
		// 생성할 때 한 번만. 실제로 보이는 부분으로 슬롯 크기를 맞춤
		RenderTexture previous = RenderTexture.active;
		Texture2D image = new Texture2D(_texture.width, _texture.height, TextureFormat.RGBA32, false);
		try
		{
			RenderTexture.active = _texture;
			image.ReadPixels(new Rect(0, 0, _texture.width, _texture.height), 0, 0);
			Color32[] pixels = image.GetPixels32();
			int left = image.width, right = -1, bottom = image.height, top = -1;
			for (int y = 0; y < image.height; ++y)
			{
				for (int x = 0; x < image.width; ++x)
				{
					if (pixels[y * image.width + x].a < 32) continue;
					left = Mathf.Min(left, x); right = Mathf.Max(right, x);
					bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y);
				}
			}
			if (right < left || top < bottom) return;
			float width = (right - left + 1.0f) / image.width;
			float height = (top - bottom + 1.0f) / image.height;
			float xOffset = ((left + right + 1.0f) / (2 * image.width) - .5f) * 2 * _camera.orthographicSize * _camera.aspect;
			float yOffset = ((bottom + top + 1.0f) / (2 * image.height) - .5f) * 2 * _camera.orthographicSize;
			_camera.transform.position += _camera.transform.right * xOffset + _camera.transform.up * yOffset;
			_camera.orthographicSize *= Mathf.Max(width, height) * 1.20f;
		}
		finally
		{
			RenderTexture.active = previous;
			Destroy(image);
		}
	}
	void PlayIdleOnly(Animator _animator, string _idleState)
	{
		RuntimeAnimatorController controller = _animator.runtimeAnimatorController;
		if (controller == null) return;

		AnimationClip idleClip = null;
		AnimationClip[] clips = controller.animationClips;
		for (int i = 0; i < clips.Length; ++i)
		{
			if (clips[i].name != _idleState) continue;

			idleClip = clips[i];
			break;
		}

		_animator.Play(_idleState, 0, 0.0f);
		_animator.Update(0.0f);

		if (idleClip == null)
		{
			AnimatorClipInfo[] currentClips = _animator.GetCurrentAnimatorClipInfo(0);
			if (currentClips.Length > 0) idleClip = currentClips[0].clip;
		}

		if (idleClip == null)
		{
			Debug.LogError("Idle 애니메이션 클립을 찾지 못했습니다: " + _idleState);
			return;
		}

		m_idleController = new AnimatorOverrideController(controller);
		List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
		m_idleController.GetOverrides(overrides);
		for (int i = 0; i < overrides.Count; ++i)
		{
			overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, idleClip);
		}

		m_idleController.ApplyOverrides(overrides);
		_animator.runtimeAnimatorController = m_idleController;
		_animator.Play(_idleState, 0, 0.0f);
		_animator.Update(0.0f);
	}

	void OnDestroy()
	{
		if (m_camera != null) m_camera.targetTexture = null;
		if (m_texture != null)
		{
			m_texture.Release();
			Destroy(m_texture);
		}
		if (m_idleController != null) Destroy(m_idleController);
	}
}
