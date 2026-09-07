using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class OfflineCombatVerification
{
    const string Key="HUDCombatTests.Running";
	const string VideoDirectory="Build/Validation/VideoFrames";
    static int phase;
    static float started;
    static bool failed;
    static OfflineCombatSession session;
    static PlayerCombat combat;
    static MonsterHitbox first, second;
    static int hits;
	static int videoFrame;
	static float nextVideoFrame;
    static OfflineCombatVerification() { if(SessionState.GetBool(Key,false)) Register(); }
    public static void Run()
    {
		if(Directory.Exists(VideoDirectory))Directory.Delete(VideoDirectory,true);
		Directory.CreateDirectory(VideoDirectory);
        SessionState.SetBool(Key,true);
        EditorSceneManager.OpenScene("Assets/MYFolder/Scenes/BeginnerZoneScene.unity",OpenSceneMode.Single);
        Register(); EditorApplication.EnterPlaymode();
    }
    static void Register()
    {
        EditorApplication.update-=Tick; EditorApplication.update+=Tick;
        EditorApplication.playModeStateChanged-=Changed; EditorApplication.playModeStateChanged+=Changed;
        Application.logMessageReceived-=Log; Application.logMessageReceived+=Log;
    }
    static void Changed(PlayModeStateChange state)
    {
        if(state==PlayModeStateChange.EnteredPlayMode){phase=0;started=Time.time;videoFrame=0;nextVideoFrame=0;}
        if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);EditorApplication.Exit(failed?1:0);}
    }
    static void Log(string message,string stack,LogType type) { if(type==LogType.Exception || type==LogType.Error)failed=true; }
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);Debug.Log("[HUDTest] PASS "+message);}
    static void Next(){++phase;started=Time.time;}
    static void Click(string name)
    {
        GameObject.Find(name).GetComponent<Button>().onClick.Invoke();
    }
    static void Tick()
    {
        if(!EditorApplication.isPlaying || EditorApplication.isCompiling)return;
        try
        {
			RecordVideoFrame();
            float elapsed=Time.time-started;
            if(elapsed>25)throw new Exception("Timeout phase="+phase);
            if(phase==0 && elapsed>1)
            {
                session=UnityEngine.Object.FindObjectOfType<OfflineCombatSession>();
                Check(session!=null && session.GetPlayer()==null,"starts with prebuilt class selection, no active player");
                Capture("HUD_Selection.png"); Click("ChooseArcher"); Next();
            }
            else if(phase==1 && elapsed>.6f)
            {
                Check(session.GetPlayer().GetPlayerClass()==ePlayerClass.Archer,"selection button activates archer prefab");
                MonsterHitbox[] monsters=UnityEngine.Object.FindObjectsOfType<MonsterHitbox>();
                Check(monsters.Length==3,"exactly three active test monsters");
                first=GameObject.Find("CombatTest_Rat_1").GetComponentInChildren<MonsterHitbox>();
                second=GameObject.Find("CombatTest_Rat_2").GetComponentInChildren<MonsterHitbox>();
                combat=session.GetPlayer().GetComponent<PlayerCombat>();
                Check(combat.GetAttackRange()==10,"archer range equals twice warrior AOE radius");
                Check(!combat.TryHeavy() && !combat.TryArea(),"archer A and S reject missing target");
                Check(combat.GetHeavyCooldown()==0 && combat.GetAreaCooldown()==0,"rejected skills do not start cooldown");
                combat.CommandTarget(first,true); Next();
            }
            else if(phase==2 && first.GetHitCount()>0)
            {
                Check(first.GetLastHit()=="일반 화살","basic arrow records hit without damage calculation");
                Check(UnityEngine.Object.FindObjectOfType<ArrowShotEffect>()!=null,"shot VFX exists when instant hit is recorded");
                hits=first.GetHitCount();
                Click("Skill_A"); Check(combat.GetHeavyCooldown()>2.8f && combat.GetHeavyCooldown()<=3.02f,"A uses real three-second cooldown actual="+combat.GetHeavyCooldown()+" message="+combat.LastMessage);
                Next();
            }
            else if(phase==3 && first.GetLastHit()=="강화 화살")
            {
                if(session.GetPlayer().GetState()==ePlayerObjectState.Skill)return;
                Check(first.GetHitCount()==hits+1,"A fires exactly one enhanced arrow");
                Capture("HUD_ArcherPower.png"); Click("Skill_S");
                if(combat.GetRapidRemaining()==0)return;
                Check(combat.GetAreaCooldown()>8.8f,"S uses nine-second cooldown");
                hits=first.GetHitCount(); Next();
            }
            else if(phase==4 && elapsed>.9f)
            {
                Check(first.GetHitCount()>=hits+5,"rapid shoots repeated instant hits");
                Check(combat.GetRapidRemaining()>1.8f,"rapid channel lasts three seconds");
                CheckHUD(); Capture("HUD_ArcherRapid.png");
                Click("KillTarget"); hits=first.GetHitCount(); Next();
            }
            else if(phase==5 && elapsed>.5f)
            {
                Check(combat.GetRapidRemaining()==0 && first.GetHitCount()==hits,"target death immediately stops rapid, no later hits");
                Click("ResetMonsters"); Check(first.IsAlive() && first.GetHitCount()==0,"test reset restores monsters and hit counters");
				CombatHUD[] hudList=UnityEngine.Object.FindObjectsOfType<CombatHUD>(true);
				Check(hudList.Length==1 && GameObject.Find("CombatCanvas")!=null,"prebuilt combat UI remains single during gameplay");
                Click("ChangeClass"); Click("ChooseWarrior"); Next();
            }
            else if(phase==6 && elapsed>.5f)
            {
                Check(session.GetPlayer().GetPlayerClass()==ePlayerClass.Warrior,"switch class through saved UI");
                combat=session.GetPlayer().GetComponent<PlayerCombat>(); combat.CommandTarget(first,false); Next();
            }
            else if(phase==7 && elapsed>1 && session.GetPlayer().GetPath().Count==0)
            {
                Check(first.GetHitCount()==0,"right-click contact remains non-attacking");
                Click("Skill_S"); Check(combat.GetAreaCooldown()>9.8f,"warrior S still uses ten-second cooldown"); Next();
            }
            else if(phase==8 && elapsed>.8f)
            {
                AreaFireEffect effect=UnityEngine.Object.FindObjectOfType<AreaFireEffect>();
                Check(effect!=null && effect.transform.childCount>=50,"fire fills interior with dense distributed emitters");
                bool center=false; for(int i=0;i<effect.transform.childCount;++i)if(effect.transform.GetChild(i).localPosition.sqrMagnitude<.1f)center=true;
                Check(center,"fire includes the center");
                Check(first.GetHitCount()>0 && second.GetHitCount()>0,"filled area records multiple hits");
                CheckHUD(); Capture("HUD_WarriorFire.png"); Next();
            }
            else if(phase==9 && elapsed>2)
            {
                Click("ChangeClass"); Click("ChooseArcher"); Next();
            }
            else if(phase==10 && elapsed>.5f)
            {
                combat=session.GetPlayer().GetComponent<PlayerCombat>();
                if(combat.GetAreaCooldown()>0)return;
                combat.CommandTarget(first,true); Check(combat.TryArea(),"rapid reusable after cooldown"); Next();
            }
            else if(phase==11 && elapsed>.5f)
            {
                combat.CommandTarget(second,true);
                Check(combat.GetTarget()==second && combat.GetRapidRemaining()==0,"changing target cancels old channel and selects new target");
                Next();
            }
            else if(phase==12 && elapsed>2)
            {
                if(combat.GetAreaCooldown()>0)return;
                Check(second.GetHitCount()>0,"basic attack continues on new target");
                Check(combat.TryArea(),"start uninterrupted rapid channel"); hits=second.GetHitCount(); Next();
            }
            else if(phase==13 && elapsed>4.6f)
            {
                Check(combat.GetRapidRemaining()==0,"rapid finishes after three seconds");
                Check(second.GetHitCount()>=hits+16,"uninterrupted rapid produces repeated hits for full duration");
                Check(second.GetLastHit()=="일반 화살","basic resumes after full rapid channel");
                Check(!failed,"no runtime errors"); Debug.Log("[HUDTest] COMPLETE"); EditorApplication.ExitPlaymode();
            }
        }
        catch(Exception e){failed=true;Debug.LogError("[HUDTest] FAIL phase="+phase+" "+e);SessionState.SetBool(Key,false);EditorApplication.Exit(1);}
    }
    static void CheckHUD()
    {
        Transform slot=GameObject.Find("Skill_S").transform;
        Image mask=slot.Find("CooldownMask").GetComponent<Image>();
        string seconds=slot.Find("Seconds").GetComponent<TMPro.TMP_Text>().text;
        Check(mask.gameObject.activeSelf && mask.fillAmount>0 && mask.fillAmount<=1,"gray radial cooldown overlay tracks actual cooldown");
        Check(seconds==Mathf.CeilToInt(combat.GetAreaCooldown()).ToString(),"cooldown seconds match combat timer");
        Check(GameObject.Find("HealthText").GetComponent<TMPro.TMP_Text>().text=="1000 / 1000","HUD reads fixed HP from PlayerInfo");
		Check(GameObject.Find("ExperienceText").GetComponent<TMPro.TMP_Text>().text=="EXP  0 / 10000","prebuilt experience HUD reads PlayerInfo progress");
    }
    static void Capture(string name)
    {
        Camera camera=Camera.main; RenderTexture rt=RenderTexture.GetTemporary(1280,720,24);
        RenderTexture old=camera.targetTexture; RenderTexture active=RenderTexture.active;
        camera.targetTexture=rt; Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active=rt;
        Texture2D image=new Texture2D(1280,720,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,1280,720),0,0); image.Apply();
        File.WriteAllBytes("Build/Validation/"+name,image.EncodeToPNG()); camera.targetTexture=old;RenderTexture.active=active;
        RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(image);
    }

	static void RecordVideoFrame()
	{
		if(Time.realtimeSinceStartup<nextVideoFrame)return;
		nextVideoFrame=Time.realtimeSinceStartup+1.0f/6.0f;
		Camera camera=Camera.main;
		if(camera==null)return;
		RenderTexture rt=RenderTexture.GetTemporary(960,540,24);
		RenderTexture old=camera.targetTexture;
		RenderTexture active=RenderTexture.active;
		camera.targetTexture=rt;
		Canvas.ForceUpdateCanvases();
		camera.Render();
		RenderTexture.active=rt;
		Texture2D image=new Texture2D(960,540,TextureFormat.RGB24,false);
		image.ReadPixels(new Rect(0,0,960,540),0,0);
		image.Apply();
		File.WriteAllBytes(VideoDirectory+"/frame_"+videoFrame.ToString("0000")+".png",image.EncodeToPNG());
		++videoFrame;
		camera.targetTexture=old;
		RenderTexture.active=active;
		RenderTexture.ReleaseTemporary(rt);
		UnityEngine.Object.DestroyImmediate(image);
	}
}
