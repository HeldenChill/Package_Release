using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AdsData", menuName = "ScriptableObjects/AdsData")]
public class AdsData : ScriptableObject
{
    [SerializeField]
    int startInterLevel;
    [SerializeField]
    int showInterLevelStep;
    [SerializeField]
    int interCappingTime;
    [SerializeField]
    int adsCappingCount = -1;
    public int StartInterLevel => startInterLevel;
        public int ShowInterLevelStep => showInterLevelStep;
    public float InterCappingTime => interCappingTime;
    public int AdsCappingCount => adsCappingCount;
}

