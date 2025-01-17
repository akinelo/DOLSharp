/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */
using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.PropertyCalc;
using log4net;
using DOL.AI.Brain;
using System;
using System.Collections.Generic;

namespace DOL.GS.Spells
{
	public abstract class PropertyChangingSpell : SpellHandler
	{
		private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		
		public override void FinishSpellCast(GameLiving target)
		{
			m_caster.Mana -= PowerCost(target);
			base.FinishSpellCast(target);
		}

		protected override int CalculateEffectDuration(GameLiving target, double effectiveness)
		{
			double duration = Spell.Duration;
			if (HasPositiveEffect)
			{	
				duration *= (1.0 + m_caster.GetModified(eProperty.SpellDuration) * 0.01);
				if (Spell.InstrumentRequirement != 0)
				{
					InventoryItem instrument = Caster.AttackWeapon;
					if (instrument != null)
					{
						duration *= 1.0 + Math.Min(1.0, instrument.Level / (double)Caster.Level); // up to 200% duration for songs
						duration *= instrument.Condition / (double)instrument.MaxCondition * instrument.Quality / 100;
					}
				}
				if (duration < 1)
					duration = 1;
				else if (duration > (Spell.Duration * 4))
					duration = (Spell.Duration * 4);
				return (int)duration; 
			}
			duration = base.CalculateEffectDuration(target, effectiveness);
			return (int)duration;
		}

		public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
		{
			// vampiir, they cannot be buffed except with resists/armor factor/ haste / power regen
			GamePlayer player = target as GamePlayer;
			if (player != null)
			{
				if (HasPositiveEffect && player.CharacterClass.ID == (int)eCharacterClass.Vampiir && m_caster != player)
				{
					if (this is StrengthBuff || this is DexterityBuff || this is ConstitutionBuff || this is QuicknessBuff || this is StrengthConBuff || this is DexterityQuiBuff || this is AcuityBuff)
					{
						GamePlayer caster = m_caster as GamePlayer;
						if (caster != null)
						{
							caster.Out.SendMessage("Your buff has no effect on the Vampiir!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
						}
						player.Out.SendMessage("This buff has no effect on you!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
						return;
					}
					if (this is ArmorFactorBuff)
					{
						if (SpellHandler.FindEffectOnTarget(target, "ArmorFactorBuff") != null && m_spellLine.IsBaseLine != true)
						{
							MessageToLiving(target, "You already have this effect!", eChatType.CT_SpellResisted);
							return;
						}
					}
				}
				
				if (this is HeatColdMatterBuff || this is AllMagicResistsBuff)
				{
					if (this.Spell.Frequency <= 0)
					{
						GameSpellEffect Matter = FindEffectOnTarget(player, "MatterResistBuff");
						GameSpellEffect Cold = FindEffectOnTarget(player, "ColdResistBuff");
						GameSpellEffect Heat = FindEffectOnTarget(player, "HeatResistBuff");
						if (Matter != null || Cold != null || Heat != null)
						{
							MessageToCaster(target.Name + " already has this effect", eChatType.CT_SpellResisted);
							return;
						}
					}
				}
				
				if (this is BodySpiritEnergyBuff || this is AllMagicResistsBuff)
				{
					if (this.Spell.Frequency <= 0)
					{
						GameSpellEffect Body = FindEffectOnTarget(player, "BodyResistBuff");
						GameSpellEffect Spirit = FindEffectOnTarget(player, "SpiritResistBuff");
						GameSpellEffect Energy = FindEffectOnTarget(player, "EnergyResistBuff");
						if (Body != null || Spirit != null || Energy != null)
						{
							MessageToCaster(target.Name + " already has this effect", eChatType.CT_SpellResisted);
							return;
						}
					}
				}
			}

			base.ApplyEffectOnTarget(target, effectiveness);
		}

		public override void OnEffectStart(GameSpellEffect effect)
		{
			ApplyBonus(effect.Owner, BonusCategory1, Property1, (int)(Spell.Value * effect.Effectiveness), false);
			ApplyBonus(effect.Owner, BonusCategory2, Property2, (int)(Spell.Value * effect.Effectiveness), false);
			ApplyBonus(effect.Owner, BonusCategory3, Property3, (int)(Spell.Value * effect.Effectiveness), false);
			ApplyBonus(effect.Owner, BonusCategory4, Property4, (int)(Spell.Value * effect.Effectiveness), false);
			ApplyBonus(effect.Owner, BonusCategory5, Property5, (int)(Spell.Value * effect.Effectiveness), false);
			ApplyBonus(effect.Owner, BonusCategory6, Property6, (int)(Spell.Value * effect.Effectiveness), false);
			ApplyBonus(effect.Owner, BonusCategory7, Property7, (int)(Spell.Value * effect.Effectiveness), false);
			ApplyBonus(effect.Owner, BonusCategory8, Property8, (int)(Spell.Value * effect.Effectiveness), false);
			ApplyBonus(effect.Owner, BonusCategory9, Property9, (int)(Spell.Value * effect.Effectiveness), false);
			ApplyBonus(effect.Owner, BonusCategory10, Property10, (int)(Spell.Value * effect.Effectiveness), false);

			SendUpdates(effect.Owner);

			eChatType toLiving = eChatType.CT_SpellPulse;
			eChatType toOther = eChatType.CT_SpellPulse;
			if (Spell.Pulse == 0 || !HasPositiveEffect)
			{
				toLiving = eChatType.CT_Spell;
				toOther = eChatType.CT_System;
				SendEffectAnimation(effect.Owner, 0, false, 1);
			}

			GameLiving player = null;

			if (Caster is GameNPC && (Caster as GameNPC).Brain is IControlledBrain)
				player = ((Caster as GameNPC).Brain as IControlledBrain).Owner;
			else if (effect.Owner is GameNPC && (effect.Owner as GameNPC).Brain is IControlledBrain)
				player = ((effect.Owner as GameNPC).Brain as IControlledBrain).Owner;

			if (player != null)
			{
				// Controlled NPC. Show message in blue writing to owner...

				MessageToLiving(player, String.Format(Spell.Message2,
													  effect.Owner.GetName(0, true)), toLiving);

				// ...and in white writing for everyone else.

				foreach (GamePlayer gamePlayer in effect.Owner.GetPlayersInRadius(WorldMgr.INFO_DISTANCE))
					if (gamePlayer != player)
						MessageToLiving(gamePlayer, String.Format(Spell.Message2,
																  effect.Owner.GetName(0, true)), toOther);
			}
			else
			{
				MessageToLiving(effect.Owner, Spell.Message1, toLiving);
				Message.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message2, effect.Owner.GetName(0, false)), toOther, effect.Owner);
			}
			if (ServerProperties.Properties.BUFF_RANGE > 0 && effect.Spell.Concentration > 0 && effect.SpellHandler.HasPositiveEffect && effect.Owner != effect.SpellHandler.Caster)
			{
				m_buffCheckAction = new BuffCheckAction(effect.SpellHandler.Caster, effect.Owner, effect);
				m_buffCheckAction.Start(BuffCheckAction.BUFFCHECKINTERVAL);
			}

		}

		BuffCheckAction m_buffCheckAction = null;

		public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
		{
			if (!noMessages && Spell.Pulse == 0)
			{
				MessageToLiving(effect.Owner, Spell.Message3, eChatType.CT_SpellExpires);
				Message.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message4, effect.Owner.GetName(0, false)), eChatType.CT_SpellExpires, effect.Owner);
			}

			ApplyBonus(effect.Owner, BonusCategory1, Property1, (int)(Spell.Value * effect.Effectiveness), true);
			ApplyBonus(effect.Owner, BonusCategory2, Property2, (int)(Spell.Value * effect.Effectiveness), true);
			ApplyBonus(effect.Owner, BonusCategory3, Property3, (int)(Spell.Value * effect.Effectiveness), true);
			ApplyBonus(effect.Owner, BonusCategory4, Property4, (int)(Spell.Value * effect.Effectiveness), true);
			ApplyBonus(effect.Owner, BonusCategory5, Property5, (int)(Spell.Value * effect.Effectiveness), true);
			ApplyBonus(effect.Owner, BonusCategory6, Property6, (int)(Spell.Value * effect.Effectiveness), true);
			ApplyBonus(effect.Owner, BonusCategory7, Property7, (int)(Spell.Value * effect.Effectiveness), true);
			ApplyBonus(effect.Owner, BonusCategory8, Property8, (int)(Spell.Value * effect.Effectiveness), true);
			ApplyBonus(effect.Owner, BonusCategory9, Property9, (int)(Spell.Value * effect.Effectiveness), true);
			ApplyBonus(effect.Owner, BonusCategory10, Property10, (int)(Spell.Value * effect.Effectiveness), true);


			SendUpdates(effect.Owner);

			if (m_buffCheckAction != null)
			{
				m_buffCheckAction.Stop();
				m_buffCheckAction = null;
			}

			return base.OnEffectExpires(effect, noMessages);
		}

		protected virtual void SendUpdates(GameLiving target)
		{
		}

		protected IPropertyIndexer GetBonusCategory(GameLiving target, eBuffBonusCategory categoryid)
		{
			IPropertyIndexer bonuscat = null;
			switch (categoryid)
			{
				case eBuffBonusCategory.BaseBuff:
					bonuscat = target.BaseBuffBonusCategory;
					break;
				case eBuffBonusCategory.SpecBuff:
					bonuscat = target.SpecBuffBonusCategory;
					break;
				case eBuffBonusCategory.Debuff:
					bonuscat = target.DebuffCategory;
					break;
				case eBuffBonusCategory.Other:
					bonuscat = target.BuffBonusCategory4;
					break;
				case eBuffBonusCategory.SpecDebuff:
					bonuscat = target.SpecDebuffCategory;
					break;
				case eBuffBonusCategory.AbilityBuff:
					bonuscat = target.AbilityBonus;
					break;
				default:
					if (log.IsErrorEnabled)
						log.Error("BonusCategory not found " + categoryid + "!");
					break;
			}
			return bonuscat;
		}

		public abstract eProperty Property1 { get; }
		public virtual eProperty Property2 => eProperty.Undefined;
		public virtual eProperty Property3 => eProperty.Undefined;
		public virtual eProperty Property4 => eProperty.Undefined;
		public virtual eProperty Property5 => eProperty.Undefined;
		public virtual eProperty Property6 => eProperty.Undefined;
		public virtual eProperty Property7 => eProperty.Undefined;
		public virtual eProperty Property8 => eProperty.Undefined;
		public virtual eProperty Property9 => eProperty.Undefined;
		public virtual eProperty Property10 => eProperty.Undefined;

		public virtual eBuffBonusCategory BonusCategory1 => eBuffBonusCategory.BaseBuff;
		public virtual eBuffBonusCategory BonusCategory2 => eBuffBonusCategory.BaseBuff;
		public virtual eBuffBonusCategory BonusCategory3 => eBuffBonusCategory.BaseBuff;
		public virtual eBuffBonusCategory BonusCategory4 => eBuffBonusCategory.BaseBuff;
		public virtual eBuffBonusCategory BonusCategory5 => eBuffBonusCategory.BaseBuff;
		public virtual eBuffBonusCategory BonusCategory6 => eBuffBonusCategory.BaseBuff;
		public virtual eBuffBonusCategory BonusCategory7 => eBuffBonusCategory.BaseBuff;
		public virtual eBuffBonusCategory BonusCategory8 => eBuffBonusCategory.BaseBuff;
		public virtual eBuffBonusCategory BonusCategory9 => eBuffBonusCategory.BaseBuff;
		public virtual eBuffBonusCategory BonusCategory10 => eBuffBonusCategory.BaseBuff;

		public override void OnEffectRestored(GameSpellEffect effect, int[] vars)
		{
			ApplyBonus(effect.Owner, BonusCategory1, Property1, vars[1], false);
			ApplyBonus(effect.Owner, BonusCategory2, Property2, vars[1], false);
			ApplyBonus(effect.Owner, BonusCategory3, Property3, vars[1], false);
			ApplyBonus(effect.Owner, BonusCategory4, Property4, vars[1], false);
			ApplyBonus(effect.Owner, BonusCategory5, Property5, vars[1], false);
			ApplyBonus(effect.Owner, BonusCategory6, Property6, vars[1], false);
			ApplyBonus(effect.Owner, BonusCategory7, Property7, vars[1], false);
			ApplyBonus(effect.Owner, BonusCategory8, Property8, vars[1], false);
			ApplyBonus(effect.Owner, BonusCategory9, Property9, vars[1], false);
			ApplyBonus(effect.Owner, BonusCategory10, Property10, vars[1], false);


			SendUpdates(effect.Owner);
		}

		public override int OnRestoredEffectExpires(GameSpellEffect effect, int[] vars, bool noMessages)
		{
			if (!noMessages && Spell.Pulse == 0)
			{
				MessageToLiving(effect.Owner, Spell.Message3, eChatType.CT_SpellExpires);
				Message.SystemToArea(effect.Owner, Util.MakeSentence(Spell.Message4, effect.Owner.GetName(0, false)), eChatType.CT_SpellExpires, effect.Owner);
			}

			ApplyBonus(effect.Owner, BonusCategory1, Property1, vars[1], true);
			ApplyBonus(effect.Owner, BonusCategory2, Property2, vars[1], true);
			ApplyBonus(effect.Owner, BonusCategory3, Property3, vars[1], true);
			ApplyBonus(effect.Owner, BonusCategory4, Property4, vars[1], true);
			ApplyBonus(effect.Owner, BonusCategory5, Property5, vars[1], true);
			ApplyBonus(effect.Owner, BonusCategory6, Property6, vars[1], true);
			ApplyBonus(effect.Owner, BonusCategory7, Property7, vars[1], true);
			ApplyBonus(effect.Owner, BonusCategory8, Property8, vars[1], true);
			ApplyBonus(effect.Owner, BonusCategory9, Property9, vars[1], true);
			ApplyBonus(effect.Owner, BonusCategory10, Property10, vars[1], true);


			SendUpdates(effect.Owner);
			return 0;
		}

		protected void ApplyBonus(GameLiving owner,  eBuffBonusCategory BonusCat, eProperty Property, int Value, bool IsSubstracted)
		{
			IPropertyIndexer tblBonusCat;
			if (Property != eProperty.Undefined)
			{
				tblBonusCat = GetBonusCategory(owner, BonusCat);
				if (IsSubstracted)
					tblBonusCat[(int)Property] -= Value;
				else
					tblBonusCat[(int)Property] += Value;
			}
		}
		
		public override PlayerXEffect GetSavedEffect(GameSpellEffect e)
		{
			PlayerXEffect eff = new PlayerXEffect();
			eff.Var1 = Spell.ID;
			eff.Duration = e.RemainingTime;
			eff.IsHandler = true;
			eff.Var2 = (int)(Spell.Value * e.Effectiveness);
			eff.SpellLine = SpellLine.KeyName;
			return eff;

		}

		public PropertyChangingSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
		{
		}

		private static Dictionary<eProperty, string> propertyToTextLookup = new Dictionary<eProperty, string>()
		{
			{eProperty.Strength, "Strength" },
			{eProperty.Constitution, "Constitution" },
			{eProperty.Dexterity, "Dexterity" },
			{eProperty.Quickness, "Quickness" },
			{eProperty.Acuity, "Acuity" },
			{eProperty.ArmorFactor, "Armor Factor (AF)" },
			{eProperty.ArmorAbsorption, "Absorption (ABS)" },
			{eProperty.WeaponSkill, "Weaponskill" },

			{eProperty.Resist_Slash, "Slash" },
			{eProperty.Resist_Crush, "Crush" },
			{eProperty.Resist_Thrust, "Thrust" },
			{eProperty.Resist_Heat, "Heat" },
			{eProperty.Resist_Cold, "Cold" },
			{eProperty.Resist_Matter, "Matter" },
			{eProperty.Resist_Body, "Body" },
			{eProperty.Resist_Spirit, "Spirit" },
			{eProperty.Resist_Energy, "Energy" },
			{eProperty.Resist_Natural, "Essence" },
		};

		protected string ConvertPropertyToText(eProperty propertyID)
		{
			if (propertyToTextLookup.TryGetValue(propertyID, out string resistText))
			{
				return resistText;
			}
			return $"<{propertyID}>";
		}
	}

	public class BuffCheckAction : RegionAction
	{
		public const int BUFFCHECKINTERVAL = 60000;//60 seconds

		private GameLiving m_caster = null;
		private GameLiving m_owner = null;
		private GameSpellEffect m_effect = null;

		public BuffCheckAction(GameLiving caster, GameLiving owner, GameSpellEffect effect)
			: base(caster)
		{
			m_caster = caster;
			m_owner = owner;
			m_effect = effect;
		}

		/// <summary>
		/// Called on every timer tick
		/// </summary>
		protected override void OnTick()
		{
			if (m_caster == null ||
			    m_owner == null ||
			    m_effect == null)
				return;

			if ( !m_caster.IsWithinRadius( m_owner, ServerProperties.Properties.BUFF_RANGE ) )
				m_effect.Cancel(false);
			else
				Start(BUFFCHECKINTERVAL);
		}
	}
}
