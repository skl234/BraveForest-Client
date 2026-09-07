using UnityEngine;

public enum eCombatAttackType : byte
{
    Basic = 0,
    SkillA,
    SkillS,
    RapidShot,
}

// 서버에도 같은 정수 공식으로 구현할 데미지 계산기입니다.
// 클라이언트 값은 숫자 연출 전용이며 실제 HP는 서버 응답으로만 변경합니다.
public static class CombatDamageCalculator
{
    public const int MaxLevel = 20;

    public static int Calculate(ePlayerClass _class, int _level, eCombatAttackType _attack)
    {
        int level = Mathf.Clamp(_level, 1, MaxLevel);
        int basicDamage;
        if (_class == ePlayerClass.Warrior) basicDamage = 22 + (level - 1) * 5;
        else basicDamage = 18 + (level - 1) * 4;

        if (_attack == eCombatAttackType.Basic) return basicDamage;
        if (_attack == eCombatAttackType.SkillA)
        {
            if (_class == ePlayerClass.Warrior) return basicDamage * 2;
            return basicDamage * 5 / 2;
        }
        if (_attack == eCombatAttackType.SkillS)
        {
            if (_class == ePlayerClass.Warrior) return basicDamage * 3 / 2;
            return basicDamage * 3 / 10;
        }
        if (_attack == eCombatAttackType.RapidShot) return basicDamage * 3 / 10;
        return 0;
    }

    public static void SplitWarriorSkillA(int _totalDamage, out int _firstDamage, out int _secondDamage)
    {
        _firstDamage = _totalDamage / 2;
        _secondDamage = _totalDamage - _firstDamage;
    }
}
