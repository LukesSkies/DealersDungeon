using System;

// Runtime result sent back by MiniGameManager.
[Serializable]
public class MiniGameResult
{
    public float score;
    public float multiplier;
    public MiniGameGrade grade;

    public bool IsGreatOrPerfect()
    {
        return grade == MiniGameGrade.Good || grade == MiniGameGrade.Perfect;
    }

    public bool IsPerfect()
    {
        return grade == MiniGameGrade.Perfect;
    }

    public static MiniGameResult None()
    {
        return new MiniGameResult
        {
            score = 0f,
            multiplier = 1f,
            grade = MiniGameGrade.Bad
        };
    }
}
