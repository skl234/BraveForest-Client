using UnityEngine;

public static class GameScreen
{
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	static void Initialize()
	{
#if !UNITY_EDITOR
		// 이전 실행에서 저장된 창 크기가 있어도 게임 화면은 동일하게
		Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
#endif
	}
}
