using UnityEngine;


[CreateAssetMenu(fileName = "PiggyBankDataSO", menuName = "HoangHH/PiggyBankDataSO")]
public class PiggyBankDataSO : ScriptableObject
{
    [SerializeField] private int passLevelRequired = 8;
    [SerializeField] private int goldCollected = 800;
    [SerializeField] private float costToBreakUsd = 2.49f;

    public int PassLevelRequired => passLevelRequired;
    public int GoldCollected => goldCollected;

    public string LocalizedCostToBreak()
    {
        // TODO: Localize this, currently only in USD
        return costToBreakUsd.ToString("F2") + "$";
    }
}
