using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Sirenix.OdinInspector;
using UnityEngine;

namespace SStats
{
	[Serializable]
	public class Stat
	{
		public float BaseValue;
		[HideInInspector]
		public bool isDirty = true;
		protected float lastBaseValue;
		[HideInInspector]
		public float _value;

		public virtual float Value
		{
			get
			{
				if (isDirty || lastBaseValue != BaseValue)
				{
					lastBaseValue = BaseValue;
					_value = CalculateFinalValue();
					isDirty = false;
				}
				return _value;
			}
		}
		[SerializeField]
		[ReadOnly]
		protected List<StatModifier> statModifiers;
		public readonly ReadOnlyCollection<StatModifier> StatModifiers;
		public Stat()
		{
			isDirty = true;
			statModifiers = new List<StatModifier>();
			StatModifiers = statModifiers.AsReadOnly();

		}
		public Stat(float baseValue) : this()
		{
			isDirty = true;
			BaseValue = baseValue;
		}
		public Stat Copy()
		{
			Stat stat = new Stat(BaseValue);
			foreach (var mod in statModifiers)
			{
				stat.AddModifier(mod);
			}
			return stat;
		}

		public void Reset()
		{
			_value = BaseValue;
			statModifiers.Clear();
			isDirty = true;
		}
		public virtual void AddModifier(StatModifier mod)
		{
			isDirty = true;
			if (statModifiers == null)
				statModifiers = new();
			statModifiers.Add(mod);
		}

		public virtual bool RemoveModifier(StatModifier mod)
		{
			if (statModifiers.Remove(mod))
			{
				isDirty = true;
				return true;
			}
			return false;
		}

		public virtual bool RemoveAllModifiersFromSource(object source)
		{
			int numRemovals = statModifiers.RemoveAll(mod => mod.Source == source);

			if (numRemovals > 0)
			{
				isDirty = true;
				return true;
			}
			return false;
		}

		public virtual bool RemoveAllModifiersExceptSource(object source)
        {
            int numRemovals = statModifiers.RemoveAll(mod => mod.Source != source);
            if (numRemovals > 0)
            {
                isDirty = true;
                return true;
            }
            return false;
        }

        public virtual List<StatModifier> GetModifiersExceptMergeFromSource(object source)
        {
            return statModifiers.FindAll(mod => mod.Source != source);
        }

        protected virtual int CompareModifierOrder(StatModifier a, StatModifier b)
		{
			if (a.Order < b.Order)
				return -1;
			else if (a.Order > b.Order)
				return 1;
			return 0; //if (a.Order == b.Order)
		}

		protected virtual float CalculateFinalValue()
		{
			float finalValue = BaseValue;
			float sumPercentAdd = 0;
			if (statModifiers == null)
			{
				statModifiers = new List<StatModifier>();
			}
			statModifiers.Sort(CompareModifierOrder);

			for (int i = 0; i < statModifiers.Count; i++)
			{
				StatModifier mod = statModifiers[i];

				if (mod.Type == StatModType.Flat)
				{
					finalValue += mod.Value;
				}
				else if (mod.Type == StatModType.PercentAdd)
				{
					sumPercentAdd += mod.Value;

					if (i + 1 >= statModifiers.Count || statModifiers[i + 1].Type != StatModType.PercentAdd)
					{
						finalValue *= 1 + sumPercentAdd;
						sumPercentAdd = 0;
					}
				}
				else if (mod.Type == StatModType.PercentMult)
				{
					finalValue *= 1 + mod.Value;
				}
			}

			// Workaround for float calculation errors, like displaying 12.00001 instead of 12

			return (float)Math.Round(finalValue, 4);
		}

	}
}
