using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

[InitializeOnLoad]
public static class PointerVerification
{
    const string Key="PointerVerification.Running";
    static int phase;
    static float started;
    static bool failed;
    static OfflineCombatSession session;
    static LocalPlayerObject player;
    static WorldPointerInput input;
    static PlayerCombat combat;
    static MonsterHitbox target;
    static Vector3 destination;
    static int count;
    static ArrowShotEffect tracer;
    static Vector3 start,end;
    static PointerVerification(){if(SessionState.GetBool(Key,false))Register();}
    public static void Run(){SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/MYFolder/Scenes/BeginnerZoneScene.unity",OpenSceneMode.Single);Register();EditorApplication.EnterPlaymode();}
    static void Register(){EditorApplication.update-=Tick;EditorApplication.update+=Tick;EditorApplication.playModeStateChanged-=Changed;EditorApplication.playModeStateChanged+=Changed;Application.logMessageReceived-=Log;Application.logMessageReceived+=Log;}
    static void Changed(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredPlayMode){phase=0;started=Time.time;}if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);EditorApplication.Exit(failed?1:0);}}
    static void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception)failed=true;}
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);Debug.Log("[PointerTest] PASS "+message);}
    static void Next(){++phase;started=Time.time;}
    static void Tick()
    {
        if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
        try
        {
            float elapsed=Time.time-started;
            if(elapsed>25)throw new Exception("Timeout phase "+phase);
            if(phase==0&&elapsed>.8f){session=UnityEngine.Object.FindObjectOfType<OfflineCombatSession>();session.ChooseArcher();Next();}
            else if(phase==1&&elapsed>.5f)
            {
                player=session.GetPlayer();input=player.GetComponent<WorldPointerInput>();combat=player.GetComponent<PlayerCombat>();
                target=GameObject.Find("CombatTest_Rat_1").GetComponentInChildren<MonsterHitbox>();
                Check(input.SubmitRight(target,target.GetCenter(),100),"RMB target accepted");
                Check(combat.GetTarget()==target,"RMB selects monster");
                Check(UnityEngine.Object.FindObjectOfType<DestinationIndicator>()==null,"target clicks do not create destination marker");
                RPGCursor cursor=player.GetComponent<RPGCursor>();cursor.SetTargetable(true);Check(cursor.IsTargetable(),"attack cursor state");cursor.SetTargetable(false);Check(!cursor.IsTargetable(),"normal cursor state");
                Next();
            }
            else if(phase==2&&target.GetHitCount()>0)
            {
                Check(target.GetLastHit()=="일반 화살","RMB now starts automatic attack");
                ArrowShotEffect shot=UnityEngine.Object.FindObjectOfType<ArrowShotEffect>();
                LineRenderer line=shot.GetComponentInChildren<LineRenderer>();
                Check(shot.GetComponentsInChildren<LineRenderer>().Length==3,"basic arrow uses a thin core and two trajectory layers without hit effect");
                Check(Vector3.Distance(line.GetPosition(line.positionCount-1),target.GetCenter())<.01f,"shot instantly spans all the way to target");
                Capture("Pointer_Tracer.png");input.SubmitLeft(null);count=target.GetHitCount();
                Check(combat.GetTarget()==null,"field LMB clears target");Next();
            }
            else if(phase==3&&elapsed>.6f)
            {
                Check(target.GetHitCount()==count,"clearing target stops further attacks");
                input.SubmitLeft(target);Check(combat.GetTarget()==target,"LMB still selects target");Next();
            }
            else if(phase==4&&target.GetHitCount()>count)
            {
                input.SubmitLeft(null);
                Vector3 requested=player.transform.position+new Vector3(3,0,0);
                Check(input.SubmitRight(null,requested,200),"first field command accepted");
                Check(!input.SubmitRight(null,requested,200.05f)&&!input.SubmitRight(null,requested,200.19f),"rapid clicking cannot bypass 0.2s gate");
                Check(input.SubmitRight(null,requested,200.2f),"next command accepted after 0.2s");
                int accepted=0;for(int i=0;i<60;++i)if(input.SubmitRight(null,requested,300+i/60.0f))++accepted;
                Check(accepted==5,"held input at 60fps produces five commands per second, actual="+accepted);
                destination=input.LastDestination;Next();
            }
            else if(phase==5&&elapsed>.1f)
            {
                DestinationIndicator marker=UnityEngine.Object.FindObjectOfType<DestinationIndicator>();
                Check(marker!=null&&Vector3.Distance(marker.transform.position,destination+Vector3.up*.06f)<.01f,"marker is at final path destination");
                Capture("Pointer_Destination.png");Next();
            }
            else if(phase==6&&elapsed>1)
            {
                Check(UnityEngine.Object.FindObjectOfType<DestinationIndicator>()==null,"marker disappears without waiting for arrival");
                Check(Vector3.Distance(new Vector2(player.transform.position.x,player.transform.position.z),new Vector2(destination.x,destination.z))<.15f,"manual path reaches final destination");
                Vector3 blocked=Vector3.zero;bool found=false;
                for(int x=-12;x<=12&&!found;++x)for(int z=-12;z<=12;++z)
                {
                    Vector3 p=player.transform.position+new Vector3(x,0,z);
                    if(NavMesh.SamplePosition(p,out NavMeshHit nearby,8,NavMesh.AllAreas)&&Vector2.Distance(new Vector2(p.x,p.z),new Vector2(nearby.position.x,nearby.position.z))>1f){blocked=p;found=true;break;}
                }
                Check(found,"found a nearby unwalkable test point");
                System.Diagnostics.Stopwatch timer=System.Diagnostics.Stopwatch.StartNew();
                Check(input.SubmitRight(null,blocked,400),"unwalkable click resolves to reachable endpoint");timer.Stop();
                destination=input.LastDestination;
                Check(NavMesh.SamplePosition(destination,out NavMeshHit hit,.1f,NavMesh.AllAreas),"corrected endpoint is on navigation surface");
                NavMeshPath path=new NavMeshPath();Check(player.GetComponent<NavMeshAgent>().CalculatePath(destination,path)&&path.status==NavMeshPathStatus.PathComplete,"corrected endpoint has complete path");
                if(NavMesh.SamplePosition(blocked,out NavMeshHit nearest,100,NavMesh.AllAreas))
                {
                    NavMeshPath nearestPath=new NavMeshPath();
                    if(player.GetComponent<NavMeshAgent>().CalculatePath(nearest.position,nearestPath)&&nearestPath.status==NavMeshPathStatus.PathComplete)
                        Check(Vector2.Distance(new Vector2(destination.x,destination.z),new Vector2(nearest.position.x,nearest.position.z))<.1f,"correction matches nearest reachable point within edge tolerance");
                }
                Debug.Log("[PointerTest] blocked="+blocked+" endpoint="+destination+" solveMs="+timer.Elapsed.TotalMilliseconds);
                Next();
            }
            else if(phase==7&&elapsed>.1f)
            {
                Capture("Pointer_CorrectedDestination.png");
                Check(input.SubmitRight(null,new Vector3(-25,.5f,-25),500),"outside map click is also corrected");
                Check(input.LastDestination.x>=0&&input.LastDestination.z>=0,"corrected path remains in positive map coordinates");
                combat.StopCombat();
                tracer=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MYFolder/Prefab/Object/Effect/ArrowPower.prefab")).GetComponent<ArrowShotEffect>();
                start=player.transform.position+Vector3.up;end=target.GetCenter();tracer.Initialize(start,end);Next();
            }
            else if(phase==8&&elapsed>.06f)
            {
                LineRenderer line=tracer.GetComponentInChildren<LineRenderer>();
                Check(tracer.GetComponentsInChildren<LineRenderer>().Length==5,"power arrow uses a thin core and four trajectory layers without hit effect");
                Check(line.GetPosition(0)==start&&line.GetPosition(line.positionCount-1)==end,"tracer endpoints stay fixed rather than flying");
                Check(line.startColor.a<1,"tracer fades in place");
                GameObject rapidPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MYFolder/Prefab/Object/Effect/ArrowRapid.prefab");
                Check(rapidPrefab.GetComponentsInChildren<LineRenderer>(true).Length==5&&rapidPrefab.transform.Find("ImpactRing_0")==null,"rapid arrow has a thin core, four trajectory layers and no hit effect");
                Capture("Pointer_PowerTrajectory.png");
                session.OpenSelection();session.ChooseWarrior();Next();
            }
            else if(phase==9&&elapsed>.3f)
            {
                player=session.GetPlayer();combat=player.GetComponent<PlayerCombat>();combat.TryArea();Next();
            }
            else if(phase==10&&elapsed>.8f)
            {
                AreaFireEffect fire=UnityEngine.Object.FindObjectOfType<AreaFireEffect>();
                Check(fire!=null&&fire.transform.Find("FireGround")==null,"fire burst has no persistent red ground quad");Capture("Pointer_FireBurst.png");Next();
            }
            else if(phase==11&&elapsed>3.1f)
            {
                Check(UnityEngine.Object.FindObjectOfType<AreaFireEffect>()==null,"fire burst fully cleans up");
                Check(!failed,"no runtime errors");Debug.Log("[PointerTest] COMPLETE");EditorApplication.ExitPlaymode();
            }
        }
        catch(Exception e){failed=true;Debug.LogError("[PointerTest] FAIL phase="+phase+" "+e);SessionState.SetBool(Key,false);EditorApplication.Exit(1);}
    }
    static void Capture(string name)
    {
        Camera camera=Camera.main;RenderTexture rt=RenderTexture.GetTemporary(1280,720,24),old=camera.targetTexture,active=RenderTexture.active;
        camera.targetTexture=rt;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;
        Texture2D image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes("Build/Validation/"+name,image.EncodeToPNG());
        camera.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(image);
    }
}
