using System;
using System.Linq;
using UnityEngine;

[Serializable]
public class BakingUpgradeLevel
{
    public int level;
    public float bakeDuration;
    public int maxStack;
    public int price;
}

[Serializable]
public class BakingConfigDTO
{
    public BakingUpgradeLevel[] upgradeLevels;
    public int maxQueue;
}

public static class BakingConfig
{
    private static BakingConfigDTO _cache;

    public static BakingConfigDTO Data
    {
        get
        {
            if (_cache == null)
            {
                var ta = Resources.Load<TextAsset>("Configs/baking_config");
                if (ta == null) throw new Exception("[BakingConfig] baking_config.json이 없습니다 (Resources/Configs).");
                _cache = JsonUtility.FromJson<BakingConfigDTO>(ta.text);
                // 레벨 정렬/검증
                Array.Sort(_cache.upgradeLevels, (a,b) => a.level.CompareTo(b.level));
            }
            return _cache;
        }
    }
    public static int MaxLevel => Data.upgradeLevels.Max(u => u.level);

    public static BakingUpgradeLevel GetLevelData(int level)
    {
        var found = Data.upgradeLevels.FirstOrDefault(u => u.level == level);
        if (found == null) throw new Exception($"[BakingConfig] 레벨 {level} 데이터가 없습니다.");
        return found;
    }
}
