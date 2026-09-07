using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// 실제 MonsterObject의 이동 함수를 일정 프레임 간격으로 실행. 테스트 씬은 저장하지 않음
public static class EntryMovementClientVerification
{
    const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;
    static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
        Debug.Log("[EntryMovementClient] PASS " + message);
    }
    static void Step(MonsterObject monster, float seconds)
    {
        typeof(MonsterObject).GetMethod("MoveServerPath", Flags).Invoke(monster, new object[] { seconds });
    }
    static void Reset(MonsterObject monster, Vector3 position)
    {
        monster.ApplyServerState(position, 0, 5);
        monster.ApplyServerState(position, 0, 0);
        monster.transform.position = position;
    }
    public static void RunCases(MonsterObject monster)
    {
        Vector3 original = monster.transform.position;
        Vector3 start = new Vector3(10, 0, 10);
        Reset(monster, start);
        List<Vector3> route = new List<Vector3> { new Vector3(15, 0, 10), new Vector3(15, 0, 15), new Vector3(10, 0, 15), start };
        monster.SetServerPath(start, 90, 4, 0, route);
        Step(monster, .25f);
        Check(Vector3.Distance(monster.transform.position, new Vector3(11, 0, 10)) < .001f, "closed U route starts at first corner, not nearest last segment");
        for (int i = 0; i < 76; ++i) Step(monster, .0625f);
        Check(Vector3.Distance(monster.transform.position, start) < .001f, "all U route corners consumed at exactly configured speed");

        Reset(monster, start);
        route = new List<Vector3> { new Vector3(15, 0, 10), new Vector3(15, 0, 15) };
        monster.SetServerPath(start, 90, 4, 0, route);
        monster.ApplyServerState(new Vector3(15, 0, 13), 180, 3);
        Step(monster, .5f);
        Check(Vector3.Distance(monster.transform.position, new Vector3(12, 0, 10)) < .001f, "early Attack state preserves obstacle corner instead of diagonal shortcut");
        Step(monster, 1.5f);
        Check(Vector3.Distance(monster.transform.position, new Vector3(15, 0, 13)) < .001f && monster.GetState() == eMonsterState.Attack, "stop point and Attack state reached without acceleration");

        Reset(monster, start);
        monster.SetServerPath(start, 90, 4, 0, route);
        monster.SetServerPath(new Vector3(15, 0, 12), 0, 4, 0, new List<Vector3> { new Vector3(19, 0, 12) });
        Step(monster, .5f);
        Check(Vector3.Distance(monster.transform.position, new Vector3(12, 0, 10)) < .001f, "changed route retains pending corner before server replan origin");
        Step(monster, 2.25f);
        Check(Vector3.Distance(monster.transform.position, new Vector3(19, 0, 12)) < .001f, "replanned path has same final destination");

        Reset(monster, new Vector3(15, 0, 12));
        monster.SetServerPath(new Vector3(15, 0, 12), 0, 4, 1, route);
        Step(monster, .5f);
        Check(Vector3.Distance(monster.transform.position, new Vector3(15, 0, 14)) < .001f, "late observer follows supplied path index, not consumed corner");

        Reset(monster, start);
        monster.SetServerPath(start, 90, 4, 0, route);
        Step(monster, .25f);
        monster.SetServerPath(start, 90, 4, 0, route);
        Step(monster, .25f);
        Check(Vector3.Distance(monster.transform.position, new Vector3(12, 0, 10)) < .001f, "same first segment snapshot does not send client back to origin");
        Vector3 stopped = monster.transform.position;
        monster.SetServerPath(stopped, 90, 4, 0, new List<Vector3>());
        Step(monster, 1);
        Check(monster.transform.position == stopped && monster.GetState() == eMonsterState.Idle, "empty route clears movement and walk animation");
        Reset(monster, original);
    }
}
