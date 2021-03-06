using GW2Scratch.EVTCAnalytics.Model.Skills;

namespace GW2Scratch.EVTCAnalytics.Statistics.RotationItems
{
	public class SkillCastItem : RotationItem
	{
		public Skill Skill { get; }
		public SkillCastType Type { get; }

		public long CastEndTime { get; }
		public long Duration => CastEndTime - ItemTime;

		public SkillCastItem(long castStartTime, long castEndTime, SkillCastType type, Skill skill) : base(castStartTime)
		{
			CastEndTime = castEndTime;
			Type = type;
			Skill = skill;
		}
	}
}