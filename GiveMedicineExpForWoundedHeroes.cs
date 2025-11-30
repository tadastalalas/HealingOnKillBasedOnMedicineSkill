using MCM.Abstractions.Base.Global;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace HealingOnKillBasedOnMedicineSkill
{
    public class GiveMedicineExpForWoundedHeroes : CampaignBehaviorBase
    {
        private readonly MCMSettings settings = AttributeGlobalSettings<MCMSettings>.Instance ?? new MCMSettings();

        public override void RegisterEvents() => CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);

        public override void SyncData(IDataStore dataStore) { }

        private void OnHourlyTick()
        {
            if (!settings.AllHeroesRecieveMedicineExpWhenWounded)
                return;

            foreach (Hero hero in Hero.AllAliveHeroes)
            {
                if (!hero.IsHealthFull()) //  !hero.IsPrisoner  && HasFood(hero)
                {
                    if (hero.PartyBelongedTo != null || hero.PartyBelongedToAsPrisoner != null)
                    {
                        GrantMedicineExpToHero(hero);
                    }
                    else if (hero.IsPrisoner && hero.CurrentSettlement != null)
                    {
                        GrantMedicineExpToHero(hero, 0.5f);
                    }
                }
            }
        }

        private void GrantMedicineExpToHero(Hero hero, float expMultiplier = 1.0f)
        {
            if (hero == null || Campaign.Current?.Models?.CharacterDevelopmentModel == null)
                return;

            int medSkillThreshold = settings.MaxMedSkillThreshold;
            int retrievedCurrentMedSkill = hero.GetSkillValue(DefaultSkills.Medicine);

            if (retrievedCurrentMedSkill < 0)
                retrievedCurrentMedSkill = 0;

            int expNeededForFullMedLevel = Campaign.Current.Models.CharacterDevelopmentModel.GetXpAmountForSkillLevelChange(hero, DefaultSkills.Medicine, 1);

            if (retrievedCurrentMedSkill > medSkillThreshold)
                retrievedCurrentMedSkill = medSkillThreshold;

            if (retrievedCurrentMedSkill == 0)
                retrievedCurrentMedSkill = 1;

            float calculatedFloat = ((float)medSkillThreshold / (float)retrievedCurrentMedSkill) / (float)settings.ExpPercentageDivisor;
            float expToAdd = (expNeededForFullMedLevel * calculatedFloat) * expMultiplier;
            float percent = (expToAdd / expNeededForFullMedLevel) * 100;
            if (settings.UseLearningRateModifier)
                hero.AddSkillXp(DefaultSkills.Medicine, expToAdd);
            else
                hero.HeroDeveloper.AddSkillXp(DefaultSkills.Medicine, expToAdd, false, true);
            if (settings.ShowMedicineExperienceInformation)
                DisplayHeroGotMedicineExpMessage(hero, expToAdd, percent, retrievedCurrentMedSkill, expNeededForFullMedLevel);
        }

        private bool HasFood(Hero hero)
        {
            foreach (ItemRosterElement element in hero.PartyBelongedTo.ItemRoster)
            {
                if (element.EquipmentElement.Item != null && element.EquipmentElement.Item.IsFood)
                    return true;
            }
            return false;
        }

        private void DisplayHeroGotMedicineExpMessage(Hero hero, float expAmount, float percent, int currentMedSkill, int expNeededForFullMedLevel)
        {
            var message = new TextObject("{=HOKBOMS_Sa1Xq}{HERO} got Medicine skill experience: {MED_EXP} ({EXP_PERC} %)\nExperience needed in total: {MED_EXP_NEED}\nMedicine skill: {MED_SKILL}")
                .SetTextVariable("HERO", hero.Name)
                .SetTextVariable("MED_EXP", expAmount.ToString("F2"))
                .SetTextVariable("MED_SKILL", currentMedSkill)
                .SetTextVariable("MED_EXP_NEED", expNeededForFullMedLevel)
                .SetTextVariable("EXP_PERC", percent.ToString("F2"));
            InformationManager.DisplayMessage(new InformationMessage(message.ToString(), Colors.Gray));
        }
    }
}