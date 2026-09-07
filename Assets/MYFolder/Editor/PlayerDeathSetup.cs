using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class PlayerDeathSetup
{
	public static void Run()
	{
		AddDeath("WarriorCombat", "SwordShield", "Die_SwordShield");
		AddDeath("ArcherCombat", "Bow", "Die_Bow");
		AssetDatabase.SaveAssets();
		Debug.Log("[PlayerDeathSetup] Warrior/Archer non-looping death states ready");
	}

	static void AddDeath(string _controllerName, string _folder, string _clipName)
	{
		string root = "Assets/MYFolder/Animation/Combat/";
		AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(root + _controllerName + ".controller");
		if (controller == null) throw new Exception("Missing controller: " + _controllerName);
		AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(root + _clipName + ".anim");
		if (clip == null)
		{
			UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("Assets/ModularRPGHeroesPolyArt/Animations/" + _folder + "/" + _clipName + ".fbx");
			for (int i = 0; i < assets.Length; ++i)
			{
				AnimationClip source = assets[i] as AnimationClip;
				if (source == null || source.name.StartsWith("__preview__")) continue;
				clip = UnityEngine.Object.Instantiate(source);
				clip.name = _clipName;
				AssetDatabase.CreateAsset(clip, root + _clipName + ".anim");
				break;
			}
		}
		if (clip == null) throw new Exception("Missing death clip: " + _clipName);
		AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
		settings.loopTime = false;
		settings.loopBlend = false;
		AnimationUtility.SetAnimationClipSettings(clip, settings);
		AnimationUtility.SetAnimationEvents(clip, new AnimationEvent[0]);
		clip.wrapMode = WrapMode.ClampForever;
		EditorUtility.SetDirty(clip);
		AnimatorStateMachine machine = controller.layers[0].stateMachine;
		AnimatorState state = null;
		ChildAnimatorState[] states = machine.states;
		for (int i = 0; i < states.Length; ++i)
			if (states[i].state.name == _clipName) state = states[i].state;
		if (state == null) state = machine.AddState(_clipName);
		state.motion = clip;
		state.transitions = new AnimatorStateTransition[0];
		EditorUtility.SetDirty(controller);
	}
}
