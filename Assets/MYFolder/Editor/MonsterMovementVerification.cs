using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class MonsterMovementVerification
{
    const string Key = "MonsterMovementVerification.Running";
    static int phase;
    static float started;
    static bool failed, sawMove, sawIdleAfterMove;
    static float moveAnimationProgress;
    static Transform[] observedBones;
    static Quaternion[] observedRotations;
    static Vector3[] observedPositions;
    static bool poseChanged;
    static OfflineCombatSession session;
    static LocalPlayerObject player;
    static PlayerCombat combat;
    static MonsterWander[] monsters;
    static Vector3[] origins, stopped;
    static Button toggle;
    static TMP_Text label;
    static MonsterMovementVerification() { if (SessionState.GetBool(Key, false)) Register(); }
    public static void Run() { SessionState.SetBool(Key, true); EditorSceneManager.OpenScene("Assets/MYFolder/Scenes/BeginnerZoneScene.unity", OpenSceneMode.Single); Register(); EditorApplication.EnterPlaymode(); }
    static void Register() { EditorApplication.update -= Tick; EditorApplication.update += Tick; EditorApplication.playModeStateChanged -= Changed; EditorApplication.playModeStateChanged += Changed; Application.logMessageReceived -= Log; Application.logMessageReceived += Log; }
    static void Changed(PlayModeStateChange _state) { if (_state == PlayModeStateChange.EnteredPlayMode) { phase = 0; started = Time.time; } if (_state == PlayModeStateChange.EnteredEditMode) { SessionState.SetBool(Key, false); EditorApplication.Exit(failed ? 1 : 0); } }
    static void Log(string _message, string _stack, LogType _type) { if (_type == LogType.Error || _type == LogType.Exception) failed = true; }
    static void Check(bool _ok, string _message) { if (!_ok) throw new Exception(_message); Debug.Log("[MonsterMoveTest] PASS " + _message); }
    static void Next() { ++phase; started = Time.time; }
    static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        try
        {
            float elapsed = Time.time - started;
            if (elapsed > 20.0f) throw new Exception("Timeout phase " + phase);
            if (phase == 0 && elapsed > 0.8f)
            {
                session = UnityEngine.Object.FindObjectOfType<OfflineCombatSession>(); session.ChooseWarrior();
                Next();
            }
            else if (phase == 1 && elapsed > 0.3f)
            {
                player = session.GetPlayer(); combat = player.GetComponent<PlayerCombat>();
                monsters = UnityEngine.Object.FindObjectsOfType<MonsterWander>(); origins = new Vector3[monsters.Length];
                for (int i = 0; i < monsters.Length; ++i) origins[i] = monsters[i].transform.position;
                toggle = GameObject.Find("MonsterMovement").GetComponent<Button>(); label = toggle.transform.Find("Label").GetComponent<TMP_Text>();
                Check(monsters.Length == 3, "three test monsters have wander components");
                Check(!session.AreMonstersMoving() && label.text == "몬스터 이동", "monsters initially remain stopped");
                for (int i = 0; i < monsters.Length; ++i) Check(Mathf.Abs(monsters[i].GetMoveSpeed() - player.GetMoveSpeed() * 1.2f) < 0.001f, "monster speed is player speed x1.2");
                toggle.onClick.Invoke(); Check(session.AreMonstersMoving() && label.text == "몬스터 정지", "top-right button starts wandering and changes label");
                combat.CommandTarget(monsters[0].GetComponentInChildren<MonsterHitbox>(), true); Next();
            }
            else if (phase == 2)
            {
                for (int i = 0; i < monsters.Length; ++i)
                {
                    eMonsterState state = monsters[i].GetComponent<MonsterObject>().GetState();
                    if (state == eMonsterState.Move) sawMove = true;
                    if (state == eMonsterState.Move)
                    {
                        Animator animator = monsters[i].GetComponent<MonsterObject>().GetAnimator();
                        if (animator.GetCurrentAnimatorStateInfo(0).IsName(monsters[i].GetComponent<MonsterObject>().GetStateAnimation()))
                            moveAnimationProgress = Mathf.Max(moveAnimationProgress, animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
                        if (i == 0 && observedBones == null)
                        {
                            observedBones = animator.GetComponentsInChildren<Transform>();
                            observedRotations = new Quaternion[observedBones.Length];
                            observedPositions = new Vector3[observedBones.Length];
                            for (int j = 0; j < observedBones.Length; ++j) { observedRotations[j] = observedBones[j].localRotation; observedPositions[j] = observedBones[j].localPosition; }
                        }
                        else if (i == 0 && !poseChanged)
                        {
                            for (int j = 0; j < observedBones.Length; ++j)
                                if (Quaternion.Angle(observedRotations[j], observedBones[j].localRotation) > 0.5f || Vector3.Distance(observedPositions[j], observedBones[j].localPosition) > 0.001f) { poseChanged = true; break; }
                        }
                    }
                    if (sawMove && state == eMonsterState.Idle) sawIdleAfterMove = true;
                    if (Vector2.Distance(new Vector2(origins[i].x, origins[i].z), new Vector2(monsters[i].transform.position.x, monsters[i].transform.position.z)) > 4.15f)
                        throw new Exception("monster left its spawn radius");
                }
                if (elapsed < 5.0f) return;
                UnityEngine.AI.NavMeshAgent diagnosticAgent = monsters[0].GetComponent<UnityEngine.AI.NavMeshAgent>();
                bool nearby = UnityEngine.AI.NavMesh.SamplePosition(monsters[0].transform.position, out UnityEngine.AI.NavMeshHit diagnosticHit, 10.0f, UnityEngine.AI.NavMesh.AllAreas);
                string diagnostic = " state=" + monsters[0].GetComponent<MonsterObject>().GetState()
                    + " position=" + monsters[0].transform.position + " onMesh=" + diagnosticAgent.isOnNavMesh + " nearby=" + nearby + " near=" + diagnosticHit.position;
                Check(sawMove, "monster enters Move state and plays movement state" + diagnostic);
                Check(sawIdleAfterMove, "monster returns to Idle after reaching a destination");
                Check(moveAnimationProgress > 0.2f, "walk animation advances instead of restarting on its first pose");
                Check(poseChanged, "rat skeleton pose actually changes while walking");
                for (int i = 0; i < monsters.Length; ++i)
                    Check(monsters[i].GetComponent<MonsterObject>().GetAnimator().GetCurrentAnimatorStateInfo(0).IsName(monsters[i].GetComponent<MonsterObject>().GetStateAnimation()), "animation matches monster state");
                Check(true, "monsters stay near their spawn centers");
                Check(combat.PathRefreshCount >= 3, "moving target path is refreshed repeatedly");
                MonsterHitbox target = combat.GetTarget();
                Vector3 direction = target.GetCenter() - player.transform.position; direction.y = 0;
                Check(direction.sqrMagnitude < 0.01f || Vector3.Dot(player.transform.forward, direction.normalized) > 0.92f, "player faces the target's current position while attacking");
                Capture("MonsterWanderAndTracking.png");
                toggle.onClick.Invoke(); stopped = new Vector3[monsters.Length]; for (int i = 0; i < monsters.Length; ++i) stopped[i] = monsters[i].transform.position; Next();
            }
            else if (phase == 3 && elapsed > 0.7f)
            {
                Check(!session.AreMonstersMoving() && label.text == "몬스터 이동", "button stops wandering and restores label");
                for (int i = 0; i < monsters.Length; ++i)
                {
                    Check(Vector3.Distance(stopped[i], monsters[i].transform.position) < 0.01f, "stopped monster remains stationary");
                    Check(monsters[i].GetComponent<MonsterObject>().GetState() == eMonsterState.Idle, "stopped monster uses Idle state");
                }
                Check(!failed, "no runtime errors"); Debug.Log("[MonsterMoveTest] COMPLETE"); EditorApplication.ExitPlaymode();
            }
        }
        catch (Exception e) { failed = true; Debug.LogError("[MonsterMoveTest] FAIL phase=" + phase + " " + e); SessionState.SetBool(Key, false); EditorApplication.Exit(1); }
    }
    static void Capture(string _name)
    {
        Camera camera = Camera.main; RenderTexture rt = RenderTexture.GetTemporary(1280, 720, 24), old = camera.targetTexture, active = RenderTexture.active;
        camera.targetTexture = rt; Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = rt;
        Texture2D image = new Texture2D(1280, 720, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply(); File.WriteAllBytes("Build/Validation/" + _name, image.EncodeToPNG());
        camera.targetTexture = old; RenderTexture.active = active; RenderTexture.ReleaseTemporary(rt); UnityEngine.Object.DestroyImmediate(image);
    }
}
