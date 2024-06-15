using RimWorld;
using System.Linq;
using System.Xml;
using Verse;

namespace CaravanActivities
{
    public class ThingCatDefCountRangeClass : IExposable
    {
        public ThingDef thingDef;

        public IntRange countRange;

        public ThingCategoryDef categoryDef;

        public string filter;

        public string Label => GenLabel.ThingLabel(thingDef, null, countRange.RandomInRange);

        public string LabelCap => Label.CapitalizeFirst(thingDef);

        public string Summary => countRange + "x " + ((thingDef != null) ? thingDef.label : "null");

        public ThingCatDefCountRangeClass()
        {
        }

        public ThingCatDefCountRangeClass(ThingDef thingDef, IntRange count)
        {
            this.thingDef = thingDef;
            this.countRange = count;
        }

        public void ExposeData()
        {
            Scribe_Defs.Look(ref thingDef, "thingDef");
            Scribe_Values.Look<IntRange>(ref countRange, "count", IntRange.one);
        }

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            if (xmlRoot.ChildNodes.Count != 1)
            {
                Log.Error("Misconfigured ThingDefCountClass: " + xmlRoot.OuterXml);
                return;
            }
            if (xmlRoot.Name.StartsWith("contains:"))
            {
                DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "filter", xmlRoot.Name.Split(new char[] { ':' }, 2).Last() ) ;
                thingDef = DefDatabase<ThingDef>.AllDefsListForReading.Where(def => def.defName.Contains(filter)).RandomElement();
            }
            else
            {
                try
                {
                    DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "thingDef", xmlRoot.Name);
                }
                catch
                {
                    DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "categoryDef", xmlRoot.Name);
                    thingDef = DefDatabase<ThingDef>.AllDefsListForReading.Where(def => def.IsWithinCategory(categoryDef)).RandomElement();
                }
            }
            
            
            countRange = ParseHelper.FromString<IntRange>(xmlRoot.FirstChild.Value);
        }

        public override string ToString()
        {
            return "(" + countRange + "x " + ((thingDef != null) ? thingDef.defName : "null") + ")";
        }

        public override int GetHashCode()
        {
            return thingDef.shortHash + countRange.GetHashCode() << 16;
        }

    }

    public class ActivityOutcomeChanceClass : IExposable
    {
        public OutcomeDef outcome;

        public int chance;

        public string Label => outcome.label;

        public string LabelCap => Label.CapitalizeFirst();


        public ActivityOutcomeChanceClass()
        {
        }

        public ActivityOutcomeChanceClass(OutcomeDef outcome, int chance)
        {
            this.outcome = outcome;
            this.chance = chance;
        }

        public void ExposeData()
        {
            Scribe_Defs.Look(ref outcome, "caravanOutcome");
            Scribe_Values.Look<int>(ref chance, "chance", 100);
        }

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            if (xmlRoot.ChildNodes.Count != 1)
            {
                Log.Error("Misconfigured ActivityOutcomeChanceClass: " + xmlRoot.OuterXml);
                return;
            }

            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "outcome", xmlRoot.Name);
            chance = ParseHelper.FromString<int>(xmlRoot.FirstChild.Value);
        }

        public override string ToString()
        {
            return outcome.label;
        }

        public override int GetHashCode()
        {
            return outcome.shortHash + chance.GetHashCode() << 16;
        }

    }

    public class ActivitySkillLevelClass : IExposable
    {
        public SkillDef skill;

        public int level;

        public string Label => skill.label;

        public string LabelCap => Label.CapitalizeFirst();


        public ActivitySkillLevelClass()
        {
        }

        public ActivitySkillLevelClass(SkillDef skill, int level)
        {
            this.skill = skill;
            this.level = level;
        }

        public void ExposeData()
        {
            Scribe_Defs.Look(ref skill, "caravanSkill");
            Scribe_Values.Look<int>(ref level, "caravanLevel", 0);
        }

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            if (xmlRoot.ChildNodes.Count != 1)
            {
                Log.Error("Misconfigured ActivitySkillLevelClass: " + xmlRoot.OuterXml);
                return;
            }

            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "skill", xmlRoot.Name);
            level = ParseHelper.FromString<int>(xmlRoot.FirstChild.Value);
        }

        public override string ToString()
        {
            return skill.label;
        }

        public override int GetHashCode()
        {
            return skill.shortHash + level.GetHashCode() << 16;
        }

    }

}
