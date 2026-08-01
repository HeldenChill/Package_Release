using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace DesignPattern
{
    using System;
    using TMPro;

    public class ObjectContainer : MonoBehaviour
    {
        public enum RESET_ACTION
        {
            NONE = -1,
            SET_ACTIVE_FALSE = 0,
            SET_ACTIVE_TRUE = 1,
        }
        public List<List<SkillObjContainer>> Data = new List<List<SkillObjContainer>>();
        public GameObject[] MainObjects;
        public int[] Amounts;
        public Type MainScriptType;

        List<List<bool>> availableState = new List<List<bool>>();
        List<int> startFindIndex = new List<int>();

        public void OnInit()
        {
            for (int i = 0; i < MainObjects.Length; i++)
            {
                Data.Add(new List<SkillObjContainer>());
                availableState.Add(new List<bool>());
                startFindIndex.Add(0);

                Transform parent = MainObjects[i].transform.parent;
                for (int j = 0; j < Amounts[i]; j++)
                {
                    SkillObjContainer dataContain = new SkillObjContainer(j);
                    GameObject main;
                    if (j == 0)
                    {
                        main = MainObjects[i];
                    }
                    else
                    {
                        main = Instantiate(MainObjects[i], parent);
                    }
                    Component[] component = main.GetComponents<Component>();
                    foreach (Component comp in component)
                    {
                        switch (comp)
                        {
                            case Transform tf:
                                dataContain.Tf = tf;
                                break;
                            case AudioSource audioSource:
                                dataContain.AudioSource = audioSource;
                                break;
                            case TMP_Text textMeshPro:
                                dataContain.Text = textMeshPro;
                                break;
                            case MonoBehaviour monoBehaviour:
                                if (monoBehaviour.GetType() == MainScriptType)
                                {
                                    dataContain.MainScript = monoBehaviour;
                                }
                                break;
                        }
                    }
                    if (dataContain != null)
                    {
                        Data[i].Add(dataContain);
                        availableState[i].Add(true);
                    }
                }

            }
        }
        public SkillObjContainer Pop(int id)
        {
            int index = availableState[id].FindIndex(startFindIndex[id], x => x == true);
            #region FIND_START_INDEX
            if (index < 0)
            {
                startFindIndex[id] = 0;
                index = availableState[id].FindIndex(startFindIndex[id], x => x == true);
            }
            else if (index == availableState[id].Count - 1)
            {
                startFindIndex[id] = 0;
            }
            else
            {
                startFindIndex[id] = index + 1;
            }
            #endregion
            availableState[id][index] = false;
            SkillObjContainer dataCon = Data[id][index];
            return dataCon;
        }
        public void Push(int id, SkillObjContainer dataCon)
        {
            availableState[id][dataCon.Id] = true;
        }
        public void Push(int id, Transform transform)
        {
            int index = Data[id].Find(x => x.Tf == transform).Id;
            availableState[id][index] = true;
        }
        public void Push(int id, int objId)
        {
            availableState[id][objId] = true;
        }
        public void ResetContainer(RESET_ACTION action = RESET_ACTION.NONE)
        {
            #region ADD_RESET_ACTION
            Action<int, int> ResetAction = null;
            switch (action)
            {
                case RESET_ACTION.SET_ACTIVE_FALSE:
                case RESET_ACTION.SET_ACTIVE_TRUE:
                    ResetAction = (i, j) => Data[i][j].Tf.gameObject.SetActive(action == RESET_ACTION.SET_ACTIVE_FALSE ? false : true);
                    break;

            }
            #endregion
            for (int i = 0; i < availableState.Count; i++)
            {
                for (int j = 0; j < availableState[i].Count; j++)
                {
                    availableState[i][j] = true;
                    ResetAction?.Invoke(i, j);
                }
            }

            for (int i = 0; i < startFindIndex.Count; i++)
            {
                startFindIndex[i] = 0;
            }
        }
    }

    public class SkillObjContainer
    {
        public int Id
        {
            get;
            private set;
        }
        public Transform Tf;
        public Rigidbody2D RgBody;
        public AudioSource AudioSource;
        public MonoBehaviour MainScript;
        public TMP_Text Text;

        public SkillObjContainer(int id)
        {
            Id = id;
        }
    }
}